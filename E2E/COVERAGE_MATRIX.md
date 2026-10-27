# Coverage matrix — 27/10/2026

Sinh từ kết quả runtime `independent-20261027-regression/results.json`.
Tested chỉ áp dụng flow đã chạy; simulated không chứng minh hành trình người chơi tự nhiên.

| Feature | User Flow / mục tiêu | Case | Kind | Execution | Coverage |
|---|---|---|---|---|---|
| Khởi động | Khởi động scene build thực tế: Launch exe → Xác minh PID/session qua bridge → Đọc scene, camera/light → Chụp screenshot → Đóng game | E2E-001 | natural | PASS | Tested |
| Khởi động | Scene ổn định khi idle: Launch → Quan sát 2 giây → Đọc lại camera/light và log → Đóng | E2E-002 | natural | PASS | Tested |
| Khởi động | Khôi phục scene bằng driver: Launch → UnityDriver.load_scene SampleScene → Đợi hook/camera/light → Đóng | E2E-003 | simulated | PASS | Tested |
| Khởi động | Khởi chạy lại tiến trình độc lập: Launch → Chụp trạng thái → Stop → Launch lại → Kiểm tra scene ban đầu → Stop | E2E-004 | natural | PASS | Tested |
| Enemy pursuit | Enemy prefab tiến gần target: Tạo Enemy prefab + target qua hook → Quan sát FixedUpdate 0.5s | SIM-001 | simulated | PASS | Tested |
| Enemy pursuit | Target null: Tạo Enemy → Gỡ target → Quan sát 0.3s | SIM-002 | simulated | PASS | Tested |
| Enemy pursuit | Speed=0: Tạo Enemy → SetMoveSpeed(0) → Quan sát | SIM-003 | simulated | PASS | Tested |
| Enemy pursuit | Speed âm: Tạo Enemy → SetMoveSpeed(-5) → Quan sát | SIM-004 | simulated | PASS | Tested |
| Enemy pursuit | Target bị destroy: Tạo Enemy → Destroy target → Quan sát | SIM-005 | simulated | PASS | Tested |
| Enemy health | Sát thương và chết: Tạo Enemy + thêm EnemyHealth qua hook → Xác nhận HP=30 → Damage(10) → Damage(20) → Đợi Destroy | SIM-006 | simulated | PASS | Tested |
| Enemy health | Damage 0: Tạo EnemyHealth → Damage(0) | SIM-007 | simulated | PASS | Tested |
| Enemy health | Damage âm: Tạo EnemyHealth → Damage(-10) | SIM-008 | simulated | PASS | Tested |
| Enemy health | Overkill: Tạo EnemyHealth → Damage(100000) | SIM-009 | simulated | PASS | Tested |
| Enemy health | Initialize 0: Tạo EnemyHealth → InitializeHealth(0) → Damage(1) | SIM-010 | simulated | PASS | Tested |
| Enemy health | Initialize âm: Tạo EnemyHealth → InitializeHealth(-5) | SIM-011 | simulated | PASS | Tested |
| Enemy health | Reinitialize sau damage: Tạo EnemyHealth → Damage(10) → InitializeHealth(45) | SIM-012 | simulated | PASS | Tested |
| Enemy prefab wiring | Prefab nguyên trạng: Instantiate Enemy prefab không thêm Health → Quan sát | SIM-013 | simulated | PASS | Tested |
| Spawner | Spawn, cap và cleanup: Dựng Spawner + fixture Enemy có Health → Wave(interval=.1,cap=2,speed=.1,hp=40) → Đợi cap → Pause spawning → ClearEnemies | SIM-014 | simulated | PASS | Tested |
| Spawner | Thiếu enemyPrefab: Dựng spawner → DropReference(enemyPrefab) → Đợi | SIM-015 | simulated | PASS | Tested |
| Spawner | Thiếu player: Dựng spawner → DropReference(player) → Đợi | SIM-016 | simulated | PASS | Tested |
| Spawner | Thiếu enemiesParent: Dựng spawner → DropReference(enemiesParent) → Đợi | SIM-017 | simulated | PASS | Tested |
| Spawner | Thiếu camera: Dựng spawner → DropReference(gameplayCamera) → Đợi | SIM-018 | simulated | PASS | Tested |
| Spawner | Thiếu settings: Dựng spawner → DropReference(settings) → Đợi | SIM-019 | simulated | PASS | Tested |
| Spawner | Interval clamp: Dựng spawner → Wave(interval=-1,cap=10) → Quan sát timestamps | SIM-020 | simulated | PASS | Tested |
| Spawner | Cap clamp: Dựng spawner → Wave(cap=0) → Đợi | SIM-021 | simulated | PASS | Tested |
| Spawner | Minimum distance bất khả thi: Dựng spawner → Minimum distance=100000 → Đợi | SIM-022 | simulated | PASS | Tested |
| Spawner | Reconfigure wave: Dựng spawner, spawn 1 → Pause → Wave speed=3,HP=70,cap=1 | SIM-023 | simulated | PASS | Tested |
| Spawner | Spawn ngoài bốn cạnh camera: Dựng spawner Health → Seed random cố định ở hook → Spawn 32 enemy → Đọc vị trí lúc spawn | SIM-024 | simulated | PASS | Tested |
| Spawner/prefab wiring | Cap với prefab nguyên trạng: Dựng spawner dùng prefab Enemy nguyên trạng → Wave cap=2 → Đợi | SIM-025 | simulated | FAIL | Failed |
| Spawner/prefab wiring | Cleanup prefab nguyên trạng: Spawn prefab nguyên trạng → Pause → ClearEnemies | SIM-026 | simulated | FAIL | Failed |
| Contact damage | Trigger enter/stay/exit với player: Dựng Enemy prefab + EnemyContactDamage thật, PlayerHealth thật ở parent và collider con → Đưa collider vào overlap bằng Rigidbody2D; đợi OnTriggerEnter2D thật → Quan sát OnTriggerStay2D qua cooldown .75s → Đưa collider ra ngoài; đợi OnTriggerExit2D và quan sát thêm .9s | SIM-027 | simulated | PASS | Tested |
| Contact damage | Collider không có PlayerHealth: Dựng EnemyContactDamage thật và collider con không PlayerHealth, có EnemyHealth=30 làm sentinel → Đưa vào overlap; xác nhận Physics2D enter/stay thật trong .9s → Đưa ra ngoài; xác nhận exit và quan sát thêm .9s | SIM-028 | simulated | PASS | Tested |

## Chưa được kiểm thử

| Feature | Flow | Coverage | Lý do |
|---|---|---|---|
| Enemy pursuit | Start fallback tìm object tag Player; target trùng vị trí; gán lại target | Not Tested | Chưa có scenario riêng; source có nhánh này, scene hiện tại không reachable. |
| Enemy health | Hai damage liên tiếp trong cùng frame sau lethal hit; giá trị int giới hạn | Not Tested | Chưa có hook quan sát trước deferred Destroy; không suy ra từ lethal case. |
| Spawner | Enemy chết → giải phóng cap → spawn bù; reference được khôi phục sau null | Not Tested | Chưa có scenario riêng. |
| Spawner | Chính xác 16 lần retry và interval dưới frame rate không ổn định | Not Tested | Sample ở frame quan sát đầu; không đo chính xác thời điểm Instantiate hoặc số lần private loop. |
| Player prefab/input | Instantiate Player prefab và điều khiển WASD | Not Tested | PlayerController gốc và GUID đã phục hồi; Player chưa được instance trong build scene nên chưa có hành trình tự nhiên để chạy. |
| Rendering/configuration | Mọi quality tier, aspect ratio, input device, platform | Not Tested | Suite hiện ưu tiên Windows 960x540/current quality; chưa kiểm chứng các cấu hình khác. |

## Không áp dụng với source hiện tại

| Feature | Coverage | Lý do |
|---|---|---|
| UI/menu/navigation/HUD | Not Applicable | Không Canvas, EventSystem, UI prefab hoặc UI script. |
| Win/lose/restart/wave progression | Not Applicable | WaveSettings đã phục hồi nhưng không có game state/wave manager hoặc transition wiring. |
| Weapon/projectile/combat journey | Not Applicable | Không weapon/projectile source/prefab, không caller gây damage enemy. |
| Save/load/progress/user settings | Not Applicable | Không PlayerPrefs, save service hoặc data source trong gameplay source. |
| XP/level/shop/currency/pickup | Not Applicable | Not found in repository. |
| Audio/animation gameplay | Not Applicable | Không AudioSource/AudioClip/Animator/controller/animation assets. |
| URP2DSceneTemplate navigation | Not Applicable | Scene template không trong enabled build scenes, không có transition. |

Mỗi case có Preconditions/Steps/Expected/Actual/Status và artifact trong HTML/JSON của run.
Không có case Blocked trong run này. Những feature không tồn tại không được tính PASS.
