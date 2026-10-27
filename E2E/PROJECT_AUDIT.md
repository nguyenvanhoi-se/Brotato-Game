# Kiểm kê và coverage audit — 27/10/2026

Đã đối chiếu toàn bộ7 source gameplay/data, 2 scene, 2 prefab và83 file trong
Assets/Packages/ProjectSettings với snapshot đầu nhiệm vụ. Source/serialization là
bằng chứng chính; Doc/PRD và các snapshot tháng9 chỉ là tài liệu lịch sử.

## Luồng tự nhiên hiện có

EditorBuildSettings chỉ bật Assets/Scenes/SampleScene.unity. Scene gồm Main Camera
(orthographic size5, AudioListener, URP camera data) và Global Light2D. Không có
Player, Enemy, Spawner, UI, EventSystem hay manager; không có startup source nào
khởi tạo chúng. Flow thực tế có thể kiểm thử là **launch → scene/camera/light →
idle → đóng tiến trình**, cùng relaunch. Không có vòng chơi đến thắng/thua để giả định.

URP2DSceneTemplate.unity cũng chỉ camera/light và không được bật trong build.

## Owner và wiring đã kiểm tra

| Owner | Behavior trong source | Wiring / testability |
|---|---|---|
| PlayerController | Keyboard.current WASD, normalize, Rigidbody2D FixedUpdate, xoay visual | Script/meta GUID khớp Player prefab; prefab không instance trong build scene |
| PlayerHealth | HP100, cooldown.75, ignore invalid/dead/disabled damage, clamp0, Died event | Source gốc đã phục hồi; không attach Player prefab/scene; Contact fixture dùng class thật |
| EnemyController | Target/tag fallback, speed clamp≥.1, pursuit trong FixedUpdate | Có trên Enemy prefab; targetnull/speed2; không scene instance |
| EnemyHealth | HP30, ignore damage≤0/dead, Initialize clamp≥1, Destroy | Không attach Enemy prefab/scene; fixture thêm component có nhãn simulated |
| EnemyContactDamage | Trigger enter/stay → GetComponentInParent<PlayerHealth> → TakeDamage(10) | Không attach Enemy prefab; Contact fixture dùng component/Physics2D thật |
| EnemySpawner | Configure, interval≥.05, cap≥1,16 attempts/4edges, configure stats, clear health children | Không attach scene; không production caller ConfigureForWave/ClearEnemies |
| WaveSettings | Serializable class gốc,5 fields/defaults + constructor5args | Dữ liệu được phục hồi; không có WaveManager hoặc wave flow trong scene |

Giữ nguyên các source đã phục hồi và .meta; xem RESTORED_SOURCES.md về provenance.
Không thêm manager, không sửa prefab/tuning để làm test pass.

Player prefab: Untagged, Rigidbody2D gravity0/rotation constrained, CapsuleCollider
nontrigger, PlayerController; child PlayerVisual/SpriteRenderer. Không PlayerHealth.
Enemy prefab: Untagged, Rigidbody2D gravity0, trigger CapsuleCollider và EnemyController;
**không EnemyHealth/EnemyContactDamage**. AliveCount/ClearEnemies của spawner chỉ duyệt
EnemyHealth. SIM-025/026 phải quan sát vấn đề cap/cleanup của prefab nguyên trạng,
không được xfail hoặc sửa source để bỏ qua assertion.

TagManager.tags rỗng chỉ là custom tags rỗng; không chứng minh built-in Player tag
undefined. Input asset khai báo Player/UI maps nhưng không có action-map consumer
được xác minh; PlayerController đọc Keyboard trực tiếp. activeInputHandler1,
fixed timestep.02; nguồn runInBackground0 được giữ, chỉ clone E2E đổi thành true.
Packages giữ nguyên Input System1.20.0, URP17.3.0, UGUI2.0.0, Test Framework1.6.0.

## Feature → Flow → Test → Result → Coverage

| Feature | Flow | Test ID / kind | Execution và coverage |
|---|---|---|---|
| Startup lifecycle | Launch→verify scene/camera/light→PNG→close, declarative JSON | E2E-001 / natural | Xem report runtime mới nhất |
| Idle | Launch→idle2s→verify scene→close | E2E-002 / natural | Xem report runtime mới nhất |
| Scene reload | Driver LoadScene→fresh scene | E2E-003 / simulated | Không phải navigation do người chơi thực hiện |
| Relaunch | Launch→stop→new process→initial scene | E2E-004 / natural | Xem report runtime mới nhất |
| Enemy pursuit | Dựng actual prefab/target→movement/null/destroy/speed clamps | SIM-001…005 / simulated | Quan sát component thật, không phải player journey |
| Enemy HP | Damage/invalid/overkill/init/reinit→Destroy | SIM-006…012 / simulated | Quan sát HP/lifetime thật |
| Saved prefab wiring | Instantiate nguyên trạng→component/speed | SIM-013 / simulated | Không coi thiếu Health là combat hoàn chỉnh |
| Spawner lifecycle/dependencies | Configure→spawn/cap→clear; thiếu từng reference | SIM-014…019 / simulated | Actual Spawner/WaveSettings, fixture data rõ ràng |
| Spawner boundaries | Interval/cap clamp, min distance, reconfigure,4edges | SIM-020…024 / simulated | Có tolerance frame cho first-observed sample |
| Prefab regression | Saved prefab→cap và cleanup có hiệu lực | SIM-025…026 / simulated | FAIL nếu wiring hiện tại thực sự làm sai contract |
| Contact | Child collider enter/stay/cooldown/exit, non-player collider | SIM-027…028 / simulated | Actual PlayerHealth/EnemyContactDamage và callbacks Physics2D |

Catalog giữ32 IDs: **3 natural +29 simulated**. HTML/JSON/JUnit của mỗi run chứa
expected/actual/status từng ID và Tested/Failed/Not Tested/Blocked riêng biệt.
Mọi tỷ lệ theo case chỉ áp dụng catalog đã biết, không đồng nghĩa coverage toàn game.

## Gaps và Not Applicable

Sáu nhóm Not Tested được giữ: tag fallback/retarget/coincident target; damage lặp
sau chết cùng frame/giới hạn int; spawn bù/khôi phục reference; exact retry count;
Player prefab/input journey; cấu hình platform/quality/aspect/device khác.

Bảy nhóm Not Applicable hiện tại: UI/menu/HUD/navigation; win/lose/wave progression;
weapon/projectile/combat journey; save/load/progress; XP/level/shop/currency/pickup;
gameplay audio/animation; navigation đến template scene. Không được tính PASS.

UI click và keyboard injection có implementation trong bridge nhưng không tự chứng
minh UI/input feature của game được bao phủ. Chưa có UI thật để kiểm chứng UI journey.

## Cơ chế kiểm thử hiện tại

Framework hiện độc lập: Python/pytest ↔ authenticated loopback TCP ↔ main-thread
Unity E2EBridge trong development build. Không có Desktop/Server/license bên ngoài.
Actual snapshots, log và PNG được lấy từ đúng process/nonce/fingerprint. Source
project không bị instrument tại chỗ. Skill CLI đã được một agent độc lập dùng để
audit; validator hợp lệ và audit exit0 không được tính vào runtime PASS.

Kết quả thực thi và giới hạn mới nhất: VERIFICATION.md. Historical reports trước
migration được giữ trong .e2e/reports và bản ZIP backup; không dùng trạng thái
Blocked/license lịch sử để mô tả runner mới.
