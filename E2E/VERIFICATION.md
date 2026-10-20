# Xác minh E2E — 20/10/2026, nhánh 20_10

**Unity test build: PASS. AltTester: Disconnected.
Số E2E case thực sự vào call phase: 0/32.**

| PASS | FAIL | SKIP thông thường | BLOCKED |
|---:|---:|---:|---:|
| 0 | 0 | 0 | 32 |

Pipeline exit code **2**. Pytest thu thập đủ 32 items nhưng
preflight chặn trước launch/connection/test assertions. JUnit/Pytest nguyên bản
ghi 32 skipped vì không có Blocked native; HTML/JSON và XML status properties
giữ đúng **32 BLOCKED**. Không có game E2E PASS hoặc gameplay FAIL được thực thi.

## Những việc đã hoàn thành

- Khôi phục nguyên WaveSettings từ upstream commit
  `1c8bc664c1433b9ce8f47fb4c1ca6c045ac042df`; giữ schema, defaults và constructor.
- Khôi phục PlayerHealth và PlayerController gốc cùng .meta từ
  `D:/LT game/CKGame/game_brotato`. Hash khớp nguyên file backup; GUID Controller
  khớp Player prefab hiện có. Không có duplicate class hoặc duplicate Assets GUID.
- Không tạo implementation giả, không đổi Scene/Prefab, tuning, package hoặc
  Unity version. Player vẫn chưa được instance vào SampleScene.
- Sửa pipeline Editor defines bằng hai lượt configure/build; SetWave hook dùng
  constructor thật. Hoàn thiện hai ID Contact đã có SIM-027/028, không thêm ID.
- Unity **6000.3.22f1** thực sự compile/build Windows Development thành công trên
  clone có source/hook fingerprint khớp. Configure exit0 và build exit0.
- 5/5 infrastructure tests đạt; đây không phải game E2E PASS. Xem infrastructure.log.

Provenance, hash và quyết định phạm vi: `RESTORED_SOURCES.md`.

## Bằng chứng build và lần thử lại

- Build cuối: `.e2e/reports/20_10-build-retry/build/BrotatoE2E.exe` và manifest
  `.exe.e2e.json`. Log có `E2E build succeeded. Manifest:` và Unity exit0.
- Source fingerprint: `c3dbb35e892b076f7b23a4b3e738908ed6fb87653dd70bd9999f943e55811353`.
- Test/build fingerprint: `e077440f8b5778412a732b352ebe9eb2259187a9542b157163907eb2ed402f5d`.
- Lần đầu trên20_10 thất bại ở công cụ Unity ApiUpdater.MovedFromExtractor cho
  Unity.2D.Common.Runtime, không có diagnostic CSxxxx. Log được giữ nguyên tại
  `.e2e/reports/20_10-regression/unity-configure.log`.
- Thử lại đúng clone sau khi kiểm tra source/hook hash; không sửa gameplay để
  che lỗi. Lần thử lại đã configure/build thành công. Không retry test để che FAIL.
- Chỉ xác nhận test build instrumented Windows; production release và nền tảng
  khác chưa được build/xác minh trong nhiệm vụ này.

## Blocker AltTester được xác nhận

- SDK và Python driver **2.3.2**; Desktop và Server binary **2.4.0** đã cài thật.
- Desktop đã được mở và thao tác Start Server. UI báo **License key is required**;
  chưa có license hợp lệ để khởi động server. Không giả lập handshake hoặc server.
- Kiểm tra kết nối `127.0.0.1:13000` timeout; chưa có driver kết nối đúng PID/session.
  Tương thích runtime giữa Server2.4.0 và SDK2.3.2 còn Unknown / Not verified.
- Cần người dùng nhập license trực tiếp trong Desktop rồi Start Server, hoặc cung
  cấp host/port của server có license. Không gửi key vào chat hoặc commit vào repo.
- `desktop-license-required.png` là ảnh lỗi của **Desktop**, không phải screenshot game.
  Chưa có screenshot hoặc runtime observation của game để báo cáo.

## Báo cáo và cách chạy tiếp

Output cuối: `.e2e/reports/20_10-final/`:

- report.html: specification, actual, Feature→Flow→Case→Result→Coverage.
- results.json và junit.xml: summary và trạng thái Blocked tách biệt.
- pytest.xml, pytest.log, events.jsonl, context.json: bằng chứng pytest nguyên bản.
- unity-configure.log, unity-build.log và build-command.json: bằng chứng build thật.
- verification-evidence.json: đối chiếu build thành công với manifest/fingerprint;
  bổ sung metadata build PASS khi suite dùng lại executable đã build.
- desktop-license-required.png: bằng chứng server thiếu license.
- inventory.json: source/API/asset/reference hashes; source-integrity.json ở
  `.e2e/safety/` xác nhận exact source, class count và GUID không trùng.

Sau khi server sẵn sàng, chạy từ root bằng Python đã cài dependency:

```powershell
python E2E/run.py --build .e2e/reports/20_10-build-retry/build/BrotatoE2E.exe
```

Nếu sửa Assets/Packages/ProjectSettings hoặc Unity hook/build recipe, phải dùng
`--build-game` để có fingerprint mới. Không xóa FAIL hoặc thay expectations để pass.

## Git và file thay đổi

- Branch hiện tại: **20_10**, tạo trực tiếp từ **main local**
  `e5f0fc595701402d9277c33faf1dfba35f0d87dc`. Không âm thầm cập nhật main từ remote.
- Snapshot `.e2e/safety/20261020T095238735606Z/` và recovery stash
  `afdce17537d5a0026e5ed5d401bc7ccdcc46c4ac` vẫn được giữ. 130 file đã được kiểm tra
  bảo toàn từng byte ngay sau chuyển nhánh. Không reset, force push, commit/push.
- Các file hướng dẫn/ảnh/solution vốn có trên codex/13_10 được giữ trong working
  tree mới; một số hiện untracked vì main local chưa từng chứa chúng.
- Source thêm: Assets/Scripts/Wave/WaveSettings.cs (+.meta và folder.meta),
  Assets/Scripts/Player/PlayerHealth.cs và PlayerController.cs (+.meta).
- Harness sửa: E2E/Unity/Editor/E2EConfigure.cs, E2EBuild.cs;
  E2E/Unity/Runtime/E2EHarness.cs; E2E/tests/test_project_flows.py, conftest.py;
  E2E/run.py, game.py, catalog.py, reporting.py; AGENTS.md và .gitignore.
- Tài liệu: README.md, PROJECT_AUDIT.md, VERIFICATION.md, RESTORED_SOURCES.md dưới E2E.
  Các module/bootstrap/audit/identity/processes/selftests đã tạo từ lượt trước được giữ.

## Coverage còn thiếu

- Tất cả32 case runtime vẫn Blocked; không có bằng chứng player journey thành công.
- Có 6 nhóm gap Not Tested và 7 nhóm
  Not Applicable trong report. Player/input source đã phục hồi nhưng không có player
  trong scene build để thực thi natural flow.
- SampleScene chỉ camera/light, chưa có loop chiến đấu, state manager, thắng/thua,
  menu/HUD, save/load, shop/reward hoặc scene navigation. Không tạo mới chúng chỉ
  để biến test thành PASS.
- Enemy prefab thiếu Health/ContactDamage; cap/cleanup cần kiểm chứng bằng SIM-025/026.
  Đây chưa phải gameplay failure đã tái hiện.
- Physics, rendering, input, error handling, teardown khi driver đang kết nối và
  screenshot của game: **Unknown / Not verified** tới khi server hoạt động.

Không tuyên bố E2E thành công hoặc100% coverage.
