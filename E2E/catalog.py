"""Cases derived from checked-in source; simulated cases never count as player journeys."""

def case(cid, feature, goal, steps, expected, *, kind="simulated", blocker=None):
    return dict(id=cid, feature=feature, goal=goal, kind=kind,
                preconditions=("Build Windows đúng source có Unity E2E bridge; "
                               "tiến trình mới cho từng case; " +
                               ("không tạo gameplay bằng hook." if kind == "natural" else
                                "scenario dựng qua E2EHarness; không phải flow người chơi.")),
                steps=steps, expected=expected, blocker=blocker)


CASES = [
    case("E2E-001", "Khởi động", "Khởi động scene build thực tế", ["Launch exe", "Xác minh PID/session qua bridge", "Đọc scene, camera/light", "Chụp screenshot", "Đóng game"], "SampleScene, camera orthographic size 5, Global Light 2D, PNG có dữ liệu; không runtime error.", kind="natural"),
    case("E2E-002", "Khởi động", "Scene ổn định khi idle", ["Launch", "Quan sát 2 giây", "Đọc lại camera/light và log", "Đóng"], "Scene/camera/light vẫn hoạt động; không runtime error.", kind="natural"),
    case("E2E-003", "Khởi động", "Khôi phục scene bằng driver", ["Launch", "UnityDriver.load_scene SampleScene", "Đợi hook/camera/light", "Đóng"], "Load lại đúng scene, một camera chính hoạt động, không runtime error.", kind="simulated"),
    case("E2E-004", "Khởi động", "Khởi chạy lại tiến trình độc lập", ["Launch", "Chụp trạng thái", "Stop", "Launch lại", "Kiểm tra scene ban đầu", "Stop"], "Hai lần khởi động có SampleScene và camera/light; không runtime error.", kind="natural"),
    case("SIM-001", "Enemy pursuit", "Enemy prefab tiến gần target", ["Tạo Enemy prefab + target qua hook", "Quan sát FixedUpdate 0.5s"], "Khoảng cách đến target giảm, y gần 0; không runtime error."),
    case("SIM-002", "Enemy pursuit", "Target null", ["Tạo Enemy", "Gỡ target", "Quan sát 0.3s"], "Enemy không tiếp tục di chuyển."),
    case("SIM-003", "Enemy pursuit", "Speed=0", ["Tạo Enemy", "SetMoveSpeed(0)", "Quan sát"], "MoveSpeed=0.1, Enemy vẫn tiến gần target."),
    case("SIM-004", "Enemy pursuit", "Speed âm", ["Tạo Enemy", "SetMoveSpeed(-5)", "Quan sát"], "MoveSpeed=0.1, không di chuyển ngược."),
    case("SIM-005", "Enemy pursuit", "Target bị destroy", ["Tạo Enemy", "Destroy target", "Quan sát"], "Enemy ngừng di chuyển, không MissingReferenceException."),
    case("SIM-006", "Enemy health", "Sát thương và chết", ["Tạo Enemy + thêm EnemyHealth qua hook", "Xác nhận HP=30", "Damage(10)", "Damage(20)", "Đợi Destroy"], "HP 30→20; Enemy bị destroy sau lethal hit."),
    case("SIM-007", "Enemy health", "Damage 0", ["Tạo EnemyHealth", "Damage(0)"], "HP giữ nguyên 30."),
    case("SIM-008", "Enemy health", "Damage âm", ["Tạo EnemyHealth", "Damage(-10)"], "HP giữ nguyên 30; không hồi thêm."),
    case("SIM-009", "Enemy health", "Overkill", ["Tạo EnemyHealth", "Damage(100000)"], "Enemy bị destroy, không runtime error."),
    case("SIM-010", "Enemy health", "Initialize 0", ["Tạo EnemyHealth", "InitializeHealth(0)", "Damage(1)"], "Max/current clamp=1; sau damage Enemy bị destroy."),
    case("SIM-011", "Enemy health", "Initialize âm", ["Tạo EnemyHealth", "InitializeHealth(-5)"], "Max/current clamp=1."),
    case("SIM-012", "Enemy health", "Reinitialize sau damage", ["Tạo EnemyHealth", "Damage(10)", "InitializeHealth(45)"], "MaxHealth=CurrentHealth=45."),
    case("SIM-013", "Enemy prefab wiring", "Prefab nguyên trạng", ["Instantiate Enemy prefab không thêm Health", "Quan sát"], "Có controller/speed=2; không có EnemyHealth (ghi bằng chứng wiring, không coi combat hoàn chỉnh)."),
    case("SIM-014", "Spawner", "Spawn, cap và cleanup", ["Dựng Spawner + fixture Enemy có Health", "Wave(interval=.1,cap=2,speed=.1,hp=40)", "Đợi cap", "Pause spawning", "ClearEnemies"], "Hai enemy, HP40/speed.1; không vượt cap; clear về 0."),
    case("SIM-015", "Spawner", "Thiếu enemyPrefab", ["Dựng spawner", "DropReference(enemyPrefab)", "Đợi"], "Không spawn; không runtime error."),
    case("SIM-016", "Spawner", "Thiếu player", ["Dựng spawner", "DropReference(player)", "Đợi"], "Không spawn; không runtime error."),
    case("SIM-017", "Spawner", "Thiếu enemiesParent", ["Dựng spawner", "DropReference(enemiesParent)", "Đợi"], "Không spawn; AliveCount=0; ClearEnemies an toàn."),
    case("SIM-018", "Spawner", "Thiếu camera", ["Dựng spawner", "DropReference(gameplayCamera)", "Đợi"], "Không spawn; không runtime error."),
    case("SIM-019", "Spawner", "Thiếu settings", ["Dựng spawner", "DropReference(settings)", "Đợi"], "Không spawn; không runtime error."),
    case("SIM-020", "Spawner", "Interval clamp", ["Dựng spawner", "Wave(interval=-1,cap=10)", "Quan sát timestamps"], "Khoảng cách spawn ít nhất .05 scaled seconds (sai số 1 FixedUpdate)."),
    case("SIM-021", "Spawner", "Cap clamp", ["Dựng spawner", "Wave(cap=0)", "Đợi"], "Cap clamp=1; đúng một enemy khỏe."),
    case("SIM-022", "Spawner", "Minimum distance bất khả thi", ["Dựng spawner", "Minimum distance=100000", "Đợi"], "Không spawn sau các lượt retry hữu hạn; game vẫn trả lời."),
    case("SIM-023", "Spawner", "Reconfigure wave", ["Dựng spawner, spawn 1", "Pause", "Wave speed=3,HP=70,cap=1"], "Enemy đang có được cập nhật speed=3,HP=70."),
    case("SIM-024", "Spawner", "Spawn ngoài bốn cạnh camera", ["Dựng spawner Health", "Seed random cố định ở hook", "Spawn 32 enemy", "Đọc vị trí lúc spawn"], "Tất cả ngoài bounds camera+margin, z=target.z, distance≥3; quan sát đủ 4 cạnh."),
    case("SIM-025", "Spawner/prefab wiring", "Cap với prefab nguyên trạng", ["Dựng spawner dùng prefab Enemy nguyên trạng", "Wave cap=2", "Đợi"], "Tổng enemy sống không vượt cap=2; case phải FAIL nếu AliveCount bỏ sót prefab thiếu Health."),
    case("SIM-026", "Spawner/prefab wiring", "Cleanup prefab nguyên trạng", ["Spawn prefab nguyên trạng", "Pause", "ClearEnemies"], "Không còn enemy child; case phải FAIL nếu ClearEnemies bỏ sót prefab thiếu Health."),
    case("SIM-027", "Contact damage", "Trigger enter/stay/exit với player", ["Dựng Enemy prefab + EnemyContactDamage thật, PlayerHealth thật ở parent và collider con", "Đưa collider vào overlap bằng Rigidbody2D; đợi OnTriggerEnter2D thật", "Quan sát OnTriggerStay2D qua cooldown .75s", "Đưa collider ra ngoài; đợi OnTriggerExit2D và quan sát thêm .9s"], "HP 100→90 khi enter; giữ 90 trong cooldown rồi giảm 80 khi stay sau .75s; sau exit HP/callback stay ổn định ít nhất .9s; không runtime error. Simulated, không phải hành trình người chơi."),
    case("SIM-028", "Contact damage", "Collider không có PlayerHealth", ["Dựng EnemyContactDamage thật và collider con không PlayerHealth, có EnemyHealth=30 làm sentinel", "Đưa vào overlap; xác nhận Physics2D enter/stay thật trong .9s", "Đưa ra ngoài; xác nhận exit và quan sát thêm .9s"], "Không PlayerHealth được tìm thấy; EnemyHealth sentinel vẫn 30; không runtime error khi enter/stay/exit. Simulated, không phải hành trình người chơi."),
]

NOT_APPLICABLE = [
    ("UI/menu/navigation/HUD", "Không Canvas, EventSystem, UI prefab hoặc UI script."),
    ("Win/lose/restart/wave progression", "WaveSettings đã phục hồi nhưng không có game state/wave manager hoặc transition wiring."),
    ("Weapon/projectile/combat journey", "Không weapon/projectile source/prefab, không caller gây damage enemy."),
    ("Save/load/progress/user settings", "Không PlayerPrefs, save service hoặc data source trong gameplay source."),
    ("XP/level/shop/currency/pickup", "Not found in repository."),
    ("Audio/animation gameplay", "Không AudioSource/AudioClip/Animator/controller/animation assets."),
    ("URP2DSceneTemplate navigation", "Scene template không trong enabled build scenes, không có transition."),
]

# Explicit gaps: do not turn source-level branch knowledge into executed coverage.
COVERAGE_GAPS = [
    ("Enemy pursuit", "Start fallback tìm object tag Player; target trùng vị trí; gán lại target", "Chưa có scenario riêng; source có nhánh này, scene hiện tại không reachable."),
    ("Enemy health", "Hai damage liên tiếp trong cùng frame sau lethal hit; giá trị int giới hạn", "Chưa có hook quan sát trước deferred Destroy; không suy ra từ lethal case."),
    ("Spawner", "Enemy chết → giải phóng cap → spawn bù; reference được khôi phục sau null", "Chưa có scenario riêng."),
    ("Spawner", "Chính xác 16 lần retry và interval dưới frame rate không ổn định", "Sample ở frame quan sát đầu; không đo chính xác thời điểm Instantiate hoặc số lần private loop."),
    ("Player prefab/input", "Instantiate Player prefab và điều khiển WASD", "PlayerController gốc và GUID đã phục hồi; Player chưa được instance trong build scene nên chưa có hành trình tự nhiên để chạy."),
    ("Rendering/configuration", "Mọi quality tier, aspect ratio, input device, platform", "Suite hiện ưu tiên Windows 960x540/current quality; chưa kiểm chứng các cấu hình khác."),
]
