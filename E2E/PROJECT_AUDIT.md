# Kiểm kê và coverage audit — 20/10/2026

Đọc AGENTS.md, skill brotato-game và các reference architecture/gameplay/coding-rules/
unity-setup; kiểm tra lại source/serialization thay vì dùng PRD làm bằng chứng.
Đã đọc toàn bộ 7 C# gameplay/data hiện tại (4 Enemy + 2 Player + WaveSettings),
2 scene, 2 prefab; kiểm tra input asset, Packages và các settings
build/version/input/physics/time/render. `inventory.json` mỗi run hash toàn bộ file
Assets/Packages/ProjectSettings và xuất APIs, hierarchy names, GUID references.

## Hiện trạng có bằng chứng

| Feature/owner | Bằng chứng | Reachable từ build scene |
|---|---|---|
| Startup | EditorBuildSettings.asset:8-10 bật SampleScene; SampleScene.unity:126/243 có Main Camera + Global Light 2D | Chỉ candidate startup; runtime chưa chạy được |
| Enemy chase | EnemyController.cs:11/23/40; Enemy.prefab:170 GUID `18e7bd69ebdaec44daa2658a812f5bb2`; speed=2, target null | Không: prefab chưa instance trong scene |
| Enemy health | EnemyHealth.cs:12/17/33/39; max30, damage clamp, Destroy | Không attach prefab/scene, không damage caller |
| Spawner | EnemySpawner.cs:26/36/46/59/71; WaveSettings gốc đã phục hồi, 5 fields + constructor | Không attach scene; có thể dựng simulated fixture |
| Contact damage | EnemyContactDamage.cs:7/12/17 gọi PlayerHealth.TakeDamage; PlayerHealth gốc HP100/cooldown.75 | Không attach prefab/scene; fixture Contact đã có implementation |
| Player prefab | Player.prefab:111 GUID `62d4b7f376264d24f9fe7489edfe02e0`; source/meta PlayerController đã phục hồi khớp GUID | Không scene instance; PlayerHealth chưa attach vào prefab gốc |
| Input configuration | InputSystem_Actions.inputactions: Player/UI maps; activeInputHandler=1; PlayerController đọc Keyboard WASD trực tiếp | Chưa có player trong build scene; action asset không có consumer được xác minh |
| Render/physics | URP 17.3.0/current quality 5, camera ortho5; prefab gravityScale0; fixed timestep .02 | Cấu hình đọc được; output/runtime chưa kiểm chứng |
| Template scene | Settings/Scenes/URP2DSceneTemplate.unity, camera+light | Không enabled build scene, không navigation |

`TagManager.tags=[]` chỉ là custom tags rỗng. Unity có built-in Player tag;
không kết luận tag undefined. Player prefab đang Untagged, nên fallback tìm
`Player` không tự trỏ vào prefab này khi instantiate nguyên trạng.

Enemy prefab GUID `c2d12958e9d2ec94880a6cea5e68e76b` có EnemyController,
Rigidbody2D và trigger collider; **không EnemyHealth/EnemyContactDamage**.
AliveCount/ClearEnemies chỉ duyệt EnemyHealth, nên hai test SIM-025/026 kiểm tra
cap/cleanup của prefab nguyên trạng và có thể phơi bày defect khi chạy được.
Hiện chưa có gameplay runtime failure để báo từ các case này.

Đã phục hồi source thật theo `RESTORED_SOURCES.md`; không thêm manager, không
đổi scene/prefab/tuning hoặc dùng class giả cho dependency.

## Feature → flow → test mapping

| Feature | Flow | Test ID | Execution/Coverage |
|---|---|---|---|
| Startup | Launch→scene/camera/light→screenshot→stop | E2E-001 | Xem results.json; server chưa chạy thì Blocked |
| Startup idle | Launch→idle→scene còn hoạt động→stop | E2E-002 | Như trên |
| Scene reload | AltDriver.LoadScene→scene fresh | E2E-003 (simulated) | Không phải user navigation |
| Process restart | Launch→stop→launch lại→initial scene | E2E-004 | Chưa xác minh runtime |
| Enemy chase | Dựng enemy/target→FixedUpdate→null/destroy target/speed clamps | SIM-001…005 | Simulated, hiện Blocked |
| Enemy HP | Dựng Health→damage/invalid/overkill/init/reinit→Destroy | SIM-006…012 | Simulated, hiện Blocked |
| Enemy wiring | Instantiate prefab nguyên trạng→đọc component/speed | SIM-013 | Simulated, không coi thiếu Health là combat hoàn chỉnh |
| Spawner lifecycle | Configure→spawn→cap→clear | SIM-014 | Simulated, hiện Blocked |
| Spawner dependencies | Bỏ từng reference/settings→không spawn | SIM-015…019 | Simulated, hiện Blocked |
| Spawner boundaries | Interval clamp/cap clamp/impossible minimum distance/reconfigure/4 edges | SIM-020…024 | Simulated, hiện Blocked |
| Saved prefab regression | Spawn nguyên prefab→cap/clear có hiệu lực | SIM-025…026 | Chưa thực thi; không hardcode expected failure |
| Contact | Collider con enter/stay/cooldown/exit; collider không player | SIM-027…028 | Đã implement bằng actual PlayerHealth/EnemyContactDamage; runtime chờ server |

Specification chi tiết từng ID gồm tất cả trường yêu cầu nằm trong catalog và
HTML report; actual/status xuất từ pytest, không điền trước PASS.
Coverage gaps có nhãn Not Tested riêng trong catalog và report: Start tag fallback,
target trùng vị trí/gán lại, damage sau chết cùng frame/giá trị int giới hạn, spawn bù sau
chết/khôi phục reference, exact retry count, Player prefab/input chưa có flow,
cấu hình nền tảng.

## Not Applicable / Not found in repository

Không có game manager/state/wave progression; không menu/HUD/UI/scene navigation;
không weapon/projectile/reward/XP/shop/currency;
không win/lose/restart gameplay; không save/load/progress/user settings; không audio/
animation gameplay. Các mục này không được tính là Tested hoặc PASS.
Template và input declaration không nâng trạng thái chúng thành feature hoạt động.

## Bằng chứng thực thi

Lần đầu Unity crash trong sandbox; không dùng crash đó để kết luận gameplay lỗi.
Đã thử lại Unity đúng phiên bản ngoài sandbox trên clone để lấy compile log thật.
WaveSettings/PlayerHealth CS0246 đã được xử lý bằng source gốc. Lượt build
`editor-defines-fix` đã PASS. Bản cuối có PlayerController và Contact hooks được
xác minh riêng; xem `VERIFICATION.md` và artifact mới nhất ở đó.

Các mục cần runtime còn **Unknown / Not verified** khi chưa có server/license:
hook/AltDriver handshake, player graphics/physics/input, screenshot của game, mọi journey và
assertion runtime. Không có cơ sở tuyên bố 100% feature coverage.
