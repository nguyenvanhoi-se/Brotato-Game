# E2E bằng AltTester Unity SDK + Python + pytest

Hệ thống test build Windows thật, khởi động/đóng tiến trình độc lập cho từng case,
kết nối AltTester và kiểm tra dữ liệu runtime. Tất cả case có ID, mục tiêu,
preconditions, steps và expected trong `catalog.py`; actual/status được ghi từ
pytest vào HTML/XML/JSON. Xem `PROJECT_AUDIT.md` trước khi suy luận về gameplay.

Checkout hiện tại **chưa có hành trình gameplay hoàn chỉnh**. SampleScene chỉ có
camera/ánh sáng. WaveSettings, PlayerHealth và PlayerController gốc đã được phục hồi;
GUID PlayerController đã resolve đúng prefab. Xem `RESTORED_SOURCES.md` để biết
provenance và hash. Harness không tạo các type gameplay giả để build.
Các case qua hook được đánh dấu `simulated`, không tính là hành trình người chơi.

## Cài và chạy CLI

Yêu cầu Windows, Git, Python 3.10+, Unity **6000.3.22f1** cùng Windows Build Support,
license Unity sử dụng được, và **AltTester Desktop/Server đã cài, kích hoạt license
và chạy**. Máy CI cần desktop session có graphics để chạy player/screenshot.
Runner không tự cài hoặc tự kích hoạt license Desktop.

```powershell
# Từ root repository; python phải là interpreter thật, không phải Windows Store alias.
python E2E/bootstrap.py

# Chỉ inventory/case matrix, tuyệt đối không phải kết quả game PASS.
python E2E/run.py --audit-only

# Mở AltTester Desktop/Server tương thích SDK 2.3.2, có license hợp lệ,
# cho server listen 127.0.0.1:13000. Desktop/Server 2.4.0 đã cài trên máy này.
# Build + chạy toàn suite + dọn tiến trình + xuất report:
python E2E/run.py --build-game

# Chỉ định Editor nếu không cài ở đường dẫn Unity Hub mặc định:
python E2E/run.py --build-game --unity 'C:/Program Files/Unity/Hub/Editor/6000.3.22f1/Editor/Unity.exe'

# Dùng build đã tạo có manifest <exe>.e2e.json và cả hai fingerprint khớp:
python E2E/run.py --build 'D:/test-build/BrotatoE2E.exe'

# Chọn case để điều tra; phần còn lại ghi Not Tested:
python E2E/run.py --build 'D:/test-build/BrotatoE2E.exe' --case E2E-001

# Kiểm tra chính harness/report; không phải E2E của game:
python -m unittest discover -s E2E/selftests -v
```

`bootstrap.py` cài dependency vào `.e2e/deps` và sparse-clone SDK tag **2.3.2**, commit
`0c432cb67bef7930b4a080467cf128de07f1f10d`. `run.py` tự thêm thư mục dependency này
vào Python path của nó và pytest con. Không cần thay Python global. Có thể dùng
virtualenv riêng và `pip install -r E2E/requirements.txt` nếu không dùng bootstrap.
Dependency Python trực tiếp được pin; dependency bắc cầu do pip giải quyết.
Không commit SDK/download/cache. SDK và driver cùng phiên bản; kiểm tra compatibility
Desktop/Server bằng kết nối thật. Không coi TCP listener là handshake thành công.

Có thể nhờ runner mở Desktop đã cài qua argv JSON; không diễn giải bằng shell:

```powershell
python E2E/run.py --build-game --desktop-command '["C:/path/to/AltTesterDesktop.exe"]'
```

Đường dẫn/args Desktop phụ thuộc bản cài thực tế; ví dụ trên là placeholder,
không phải đường dẫn đã xác minh trên máy này. Nếu không truyền tùy chọn, runner
dùng server đang chạy và không đóng ứng dụng của người dùng. Chỉ tiến trình do
harness tạo mới được dọn theo PID; không kill Unity/game bằng tên chung.

## Pipeline và giới hạn

1. Hash toàn bộ file trong Assets, Packages và ProjectSettings; xuất inventory,
   enabled scenes, APIs, object names, script GUID references, input maps.
2. Với `--build-game`, copy ba thư mục vào `.e2e/projects/run-*/`, kiểm tra hash
   snapshot, thêm hooks vào **clone** và SDK thành embedded package của **clone**.
   Package Manager có thể tải dependency bổ sung trong clone. Không thay Unity
   version, Packages, scene, prefab hoặc tuning gốc.
3. Lượt Unity thứ nhất chạy `E2EConfigure.Configure` để lưu defines
   `ALTTESTER;BROTATO_E2E` vào clone. Lượt thứ hai mới compile/build Development
   Windows. Không dựa vào extraScriptingDefines của player để compile Editor code.
   Builder chỉ chạy khi có marker `.brotato-e2e-clone`; bật đúng scenes hiện có.
   Mỗi scene được thêm AltRunner và `__E2EHarness`, serialized field `enemyPrefab`
   nối `Assets/Prefabs/Enemy/Enemy.prefab`. Không cần thao tác Inspector ở project gốc.
4. Kiểm tra build manifest/fingerprint của source và Unity hooks/build recipe,
   dependency và server. Build cũ không được reuse sau khi thay hooks. Kể cả preflight bị
   chặn, pytest vẫn thu thập case và báo lý do Blocked; không launch player vô ích.
5. Mỗi case khả thi: mở exe mới, kết nối AltDriver qua appName `BrotatoE2E`, chạy
   xác minh PID + session nonce + hai fingerprint từ runtime rồi chạy
   assertion, ghi snapshots/actions/log; runtime errors làm test FAIL; chụp PNG
   khi thất bại nếu driver còn dùng được; dọn driver và cây tiến trình game.
   Chỉ lỗi connection lúc tạo AltDriver được phân loại Blocked; lỗi hook/JSON/
   assertion sau kết nối giữ FAIL, không bị đổi thành unavailable.
6. Timeout build mặc định 900s, toàn suite 900s; thay bằng `--build-timeout` và
   `--suite-timeout`. Timeout dừng cây PID do harness sở hữu, ghi case chưa xong
   Blocked. Có thể cần tăng suite timeout khi thêm nhiều case/thiết bị chậm.
7. Xuất report từ từng setup/call/teardown. Không retry để che lỗi, không xfail,
   không hardcode PASS, không bỏ FAIL lúc tổng hợp.

Exit **0**: các case được chọn đã chạy và đạt; **1**: pytest failure; **2** hoặc
nonzero khác: môi trường/infrastructure/Blocked/Skip/pytest error. `--audit-only`
trả 0 nghĩa là tạo inventory thành công, **không** có E2E execution.
Chạy subset có thể exit 0 nhưng các case không được chọn vẫn Not Tested.

Output mặc định `.e2e/reports/<UTC timestamp>/`; `--out <empty-folder>` cho CI.
Không tái sử dụng output có dữ liệu cũ. Giữ folder clone/build/log để điều tra;
runner dọn tiến trình nhưng không tự xóa evidence hoặc clone.

| Artifact | Nội dung |
|---|---|
| `report.html` | Test specification + actual + Feature → Flow → Case → Execution → Coverage |
| `results.json` | Summary, case status, environment, gaps, Not Applicable |
| `junit.xml` | JUnit tổng hợp, cả khi dependency/pytest thiếu |
| `pytest.xml`, `pytest.log` | Kết quả pytest gốc nếu pytest chạy |
| `inventory.json` | File hashes và bằng chứng static của snapshot |
| `unity-configure.log`, `unity-build.log`, launcher logs | Hai lượt Unity configure/compile/build thật |
| `build-command.json`, `<exe>.e2e.json` | Clone/command/build provenance |
| `<TestID>/observations.json`, `player-N.log` | Actions, snapshots và log player |
| `<TestID>/*.png`, `screenshot-error.txt` | Screenshot hoặc lỗi không thể chụp khi có driver |

JUnit không có status Blocked chuẩn: report dùng `<skipped type="BLOCKED">` và
property `status=BLOCKED`. Pytest gốc cũng thể hiện chúng là skipped. **Summary
JSON/HTML phân biệt SKIP và BLOCKED**, CI phải đọc exit code, không chỉ nhìn failures=0.
Không có player/driver thì không có ảnh game; không tạo screenshot giả.

## Phạm vi và mở rộng

32 case: 3 natural startup/idle/relaunch; 1 simulated driver scene reload;
26 scenario Enemy/health/spawner; 2 Contact scenario đã có implementation dùng
PlayerHealth/EnemyContactDamage thật và Physics2D callbacks. Không thêm ID mới.
Spawner hook gọi constructor 5 tham số của WaveSettings đã phục hồi. Build PASS
không có nghĩa các scenario đã chạy; xem execution count và Connected/Disconnected
trong report. Server 2.4.0 hiện yêu cầu nhập license; Desktop mở chưa có nghĩa server chạy.

Test sử dụng actual component và actual Enemy prefab. `CreateEnemy(true)` hoặc
`CreateSpawner(true)` **thêm EnemyHealth chỉ trong fixture simulated**; hai case
SIM-025/026 giữ nguyên prefab để kiểm tra defect wiring. Target là Transform fixture,
không phải Player giả. Hook không gọi thắng/thua hay save vì không có hệ thống đó.
Random seed là setup simulated. Spawn sample là frame quan sát đầu, tolerance vị
trí .1 và thời gian .02s; không phải đo chính xác private loop hoặc thời điểm Instantiate.

Khi thêm feature, map đường đi từ scene build đến thao tác người chơi trước;
thêm test ID + expectation riêng, có fixture reset/save sandbox theo implementation
thực tế. Không cho save test xóa PlayerPrefs/profile thật của người dùng.
Mở rộng helper thành screen/page object khi UI thật xuất hiện. Tìm source và GUID
callers trước khi đổi locator, field hoặc hook. Contact vẫn là simulated; không coi
component fixture này là một hành trình gameplay có player trong scene thật.

Inventory là scan file/API/serialization với các rule rõ ràng, **không tự chứng minh
semantic coverage** của source mới. `catalog.py:COVERAGE_GAPS` liệt kê các nhánh
chưa có case riêng; không kết luận 100% coverage từ việc tất cả case hiện có PASS.
Regression cần đọc cả coverage gaps, log, artifacts và trạng thái Not Applicable.

Nguồn API đã kiểm tra: [SDK 2.3.2](https://github.com/alttester/AltTester-Unity-SDK/tree/2.3.2),
[instrumented build](https://alttester.com/docs/sdk/latest/pages/advanced-usage.html),
[Python/pytest và Desktop prerequisite](https://alttester.com/docs/sdk/latest/pages/get-started.html),
[driver API](https://alttester.com/docs/sdk/latest/pages/commands.html).
