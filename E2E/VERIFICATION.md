# Xác minh E2E độc lập — 27/10/2026

**Unity Windows Development Build: PASS. Driver độc lập: Connected.
Đã thực thi 32/32 case trên game thật. Pipeline exit code 1.**

| PASS | FAIL | SKIP | BLOCKED | Case Not Tested |
|---:|---:|---:|---:|---:|
| 30 | 2 | 0 | 0 | 0 |

Có **3 natural + 29 simulated**.
Simulated dùng component/physics thật sau hook tạo fixture; không được tính như
hành trình người chơi tự nhiên. Không tuyên bố E2E toàn dự án thành công hoặc 100% coverage.

## Framework và gỡ công cụ cũ

- Skill `.agents/skills/unity-e2e-testing/SKILL.md`, CLI wrapper, hướng dẫn authoring/protocol;
  AGENTS.md và project skill đã route sang framework độc lập.
- Python/pytest dùng TCP loopback có token/session/PID/fingerprint, driver trong
  Windows Development build; không cần AltTester SDK/Desktop/Server/license.
- Build chỉ instrument clone `.e2e/projects/`, giữ nguyên source checkout. Mỗi case
  launch process mới, xác minh connection, quan sát state/assertion, lấy log/PNG,
  chụp failure và teardown process do runner sở hữu. Không hardcode PASS/xfail.
- Hỗ trợ JSON flow với launch, query/wait/assert, UI click, keyboard, scene,
  screenshot, error check và close. `flows/startup.json` đã chạy thật.
- Đã backup source/spec/logic, coverage và historical reports trước khi gỡ.
  Desktop2.4.0 uninstaller exit0; SDK, Server/Desktop, dedicated Python dependencies,
  config/shortcut/uninstall entries, generated clones/builds cũ đã gỡ theo whitelist.
  Xem [LEGACY_BACKUP.md](LEGACY_BACKUP.md), backup `manifest.json`, `cleanup.json`,
  `post-cleanup.json`. Historical report/ZIP và lịch sử Git được giữ làm bằng chứng.

## Bằng chứng build và thực thi

Unity **6000.3.22f1**, Python3.12.14, pytest8.3.5. Nhánh **20_10**,
source revision `d89d3c536a8e0dba4a83b170bcee500c6db7a5ac`; thay đổi hiện tại chưa commit/push.

Build và smoke (4 selected cases PASS, 28 Not Tested), exit0:

```powershell
python E2E/run.py --build-game --build-timeout 900 --case E2E-001 --case SIM-001 --case SIM-014 --case SIM-027 --out .e2e/reports/independent-20261027-smoke
```

Full regression dùng đúng build khớp manifest, exit1:

```powershell
python E2E/run.py --build .e2e/reports/independent-20261027-smoke/build/BrotatoE2E.exe --out .e2e/reports/independent-20261027-regression
```

Source fingerprint `c3dbb35e892b076f7b23a4b3e738908ed6fb87653dd70bd9999f943e55811353`.
Unity hook/build recipe fingerprint `b6ae58c7321545b696440d2ee9b9e09d596cf0080c07d76a4a28d83582377f53`.
Configure/build thực sự exit0; manifest được ghi sau BuildPipeline Succeeded.
Pytest thực sự kết thúc **2 failed, 30 passed in215.78s**, không có case bị skip/block.

- [HTML report](../.e2e/reports/independent-20261027-regression/report.html)
- [Kết quả JSON](../.e2e/reports/independent-20261027-regression/results.json)
- [JUnit XML](../.e2e/reports/independent-20261027-regression/junit.xml),
  [pytest XML](../.e2e/reports/independent-20261027-regression/pytest.xml),
  [pytest log](../.e2e/reports/independent-20261027-regression/pytest.log)
- [Unity build log](../.e2e/reports/independent-20261027-smoke/unity-build.log),
  configure log/build commands và executable+manifest cùng folder smoke.
- Per-case observations.json/player logs/PNG trong folder từng ID; failure screenshots
  SIM-025/failure-call.png và SIM-026/failure-call.png vẫn được giữ.
- [Coverage matrix](COVERAGE_MATRIX.md): Feature → Flow → Case → Result → Coverage,
  bao gồm cả gaps và Not Applicable; HTML giữ toàn bộ test specification/actual.

## Hai FAIL được giữ nguyên

**SIM-025 — cap với saved Enemy prefab:** expected tối đa2 enemy; assertion đã
quan sát5 enemy, AliveCount0. Teardown tiếp tục quan sát spawnedCount7, không runtime
error. Enemy prefab không có EnemyHealth; `EnemySpawner.AliveCount` chỉ đếm
`GetComponentsInChildren<EnemyHealth>()` (EnemySpawner.cs:14–24), nên spawn vượt cap.

**SIM-026 — ClearEnemies với saved Enemy prefab:** sau pause/clear vẫn còn2 enemy
đến hết wait timeout; AliveCount0, sourceEnemyHasHealth=false, errorCount0.
`EnemySpawner.ClearEnemies()` (EnemySpawner.cs:71–75) chỉ destroy EnemyHealth children
nên không xóa những enemy này.

Đây là hành vi source/prefab được xác minh trên fixture **simulated** của game thật,
không phải lỗi kết nối. Source/Prefab chưa được sửa để làm PASS; scene production
hiện chưa instance spawner nên chưa có bằng chứng ảnh hưởng một vòng chơi tự nhiên.

## Kiểm chứng framework và bảo toàn source

- **5/5 infrastructure selftests PASS**, log `infrastructure.log` trong folder regression.
  Đây là checks harness/report, không cộng vào game E2E PASS.
- **9 protocol checks** thực hiện với player thật: từ chối token/command/key/scene/UI
  target sai, chặn screenshot ngoài artifact root và reuse file cũ; connection phục hồi
  sau auth reject, valid Input System key command không runtime error.
  Bằng chứng `.e2e/reports/independent-20261027-protocol/checks.json` và player log/PNG.
  Không có Player trong scene nên không suy ra movement WASD từ check keyboard.
- Official skill validator PASS; skill wrapper đã được một agent độc lập chạy audit-only
  exit0. Audit không được tính game runtime PASS.
- Đối chiếu **83/83 file Assets/Packages/ProjectSettings** với hash đầu nhiệm vụ:
  không thay đổi/thêm/xóa file. WaveSettings, PlayerHealth, PlayerController và .meta
  đã khôi phục được bảo toàn. `integrity.json` ghi kết quả; ZIP backup đã kiểm CRC.

## Giới hạn và phần chưa bao phủ

SampleScene chỉ camera/light; ba natural flows hiện có là startup, idle, relaunch.
Không có Player/Enemy/Spawner/UI/manager trong enabled scene, không có play/combat/
win/lose/save/navigation loop reachable để chạy. Không xây gameplay giả thay thế.

Sáu nhóm **Not Tested**: tag fallback/retarget/coincident target; lethal damage cùng
frame/int limits; spawn bù/reference recovery; chính xác retry count/timing; Player
prefab/WASD journey; platform/quality/aspect/device khác.

Bảy nhóm **Not Applicable** theo source hiện tại: UI/menu/HUD; win/lose/wave
progression; weapon/projectile/combat journey; save/load/progress; XP/shop/currency;
audio/animation gameplay; navigation đến template scene. Chi tiết ở matrix/audit.

UI click đã có implementation nhưng chưa có UI thật để kiểm chứng positive UI journey.
Chỉ xác minh Windows960x540/current quality, không production release hoặc nền tảng khác.
Không còn blocker thương mại của pipeline; hai regression FAIL và wiring/coverage gaps
vẫn tồn tại. Muốn có hành trình chơi đến kết thúc cần gameplay thực tế được tích hợp
và test tương ứng trong nhiệm vụ sau.
