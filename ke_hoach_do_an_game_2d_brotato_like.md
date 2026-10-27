# KẾ HOẠCH ĐỒ ÁN: GAME 2D SINH TỒN (BROTATO-LIKE)

## 1. MỤC TIÊU ĐỒ ÁN

Xây dựng một game 2D top-down survival shooter lấy cảm hứng từ Brotato, nhưng giảm quy mô để phù hợp với đồ án môn Lập trình Game và người mới học Unity.

Mục tiêu chính không phải tạo thật nhiều nội dung, mà là hoàn thiện một vòng lặp gameplay rõ ràng, có cấu trúc code dễ giải thích và có khả năng mở rộng nếu còn thời gian.

### Gameplay cốt lõi

```text
Player di chuyển
    ↓
Quái spawn và đuổi Player
    ↓
Súng tự tìm mục tiêu và tự bắn
    ↓
Quái chết → tăng Kill Count → có cơ hội rơi Item
    ↓
Player sống sót hết thời gian Wave
    ↓
Chuyển sang Wave tiếp theo
    ↓
Độ khó tăng dần
    ↓
Hoàn thành Wave cuối → Victory
```

---

# 2. PHẠM VI ĐỒ ÁN

## 2.1 Phạm vi bắt buộc - MVP

Đây là các chức năng phải hoàn thành trước khi thêm bất kỳ cơ chế nâng cao nào.

### Player

- Di chuyển bằng WASD.
- Có HP.
- Nhận sát thương khi chạm quái.
- Có thời gian miễn nhiễm sát thương ngắn sau khi bị đánh.
- HP về 0 → Game Over.
- Không sử dụng hệ thống nhiều mạng.

### Weapon

- Player có 1 súng.
- Súng xoay theo hướng chuột.
- Súng tự bắn.
- Bullet gây damage lên quái.
- Súng có thể nâng cấp từ Level 1 → Level 4.

### Enemy

Tối đa 3 loại:

1. Normal Enemy
2. Fast Enemy
3. Tank Enemy

Tất cả quái chỉ cần:

- Spawn.
- Di chuyển về phía Player.
- Gây damage khi va chạm Player.
- Có HP.
- Chết khi HP <= 0.

Không làm enemy bắn đạn trong phiên bản chính.

### Wave

- Tổng cộng 9 Wave.
- Chia thành 3 khu vực:
  - Wave 1–3: Forest
  - Wave 4–6: Desert
  - Wave 7–9: Dungeon
- Mỗi Wave kéo dài khoảng 45–60 giây.
- Độ khó tăng dần.
- Hết thời gian → chuyển sang Wave tiếp theo.
- Hoàn thành Wave 9 → Victory.

### Item

Tối đa 3 loại:

- Health Pickup
- Speed Boost
- Damage Boost

Không phải enemy nào chết cũng rơi item.

Ví dụ:

```text
20% cơ hội rơi item
```

### UI

Hiển thị:

- HP
- Timer
- Wave hiện tại
- Kill Count
- Weapon Level

Các màn hình:

- Main Menu
- Pause
- Wave Complete
- Game Over
- Victory

---

# 3. CÁC CHỨC NĂNG KHÔNG LÀM TRONG PHIÊN BẢN ĐẦU

Các chức năng sau chỉ được làm khi toàn bộ MVP đã hoạt động ổn định:

- Boss
- Enemy ranged attack
- Shop
- Inventory
- Nhiều loại súng
- Đổi súng
- Character selection
- Skill tree
- Dash
- Reload
- Quest
- Achievement
- Save / Load
- Procedural map
- Multiplayer
- Online leaderboard
- Object Pooling nâng cao
- Hệ thống trạng thái phức tạp
- AI phức tạp

Nguyên tắc:

> Không thêm chức năng mới nếu gameplay loop chính chưa hoàn chỉnh.

---

# 4. GAME DESIGN

## 4.1 Thể loại

- 2D
- Top-down
- Survival Shooter
- Auto Attack
- Single Player

## 4.2 Điều khiển

```text
W / A / S / D → Di chuyển
ESC → Pause
```

Không cần:


- Click để bắn
- Reload
- Đổi vũ khí

Player chỉ tập trung vào việc di chuyển và né quái.

---

# 5. PLAYER SYSTEM

## 5.1 Player Stats

Ví dụ:

| Thuộc tính | Giá trị |
|---|---:|
| Max HP | 100 |
| Movement Speed | 5 |
| Damage Cooldown | 0.5–1 giây |

Player chỉ có một mạng.

```text
HP <= 0
    ↓
Game Over
```

## 5.2 Damage Cooldown

Sau khi nhận damage:

```text
Player bị đánh
    ↓
Trừ HP
    ↓
Miễn nhiễm sát thương khoảng 0.5–1 giây
    ↓
Có thể nhận damage tiếp
```

Có thể thêm hiệu ứng sprite nhấp nháy để player nhận biết.

---

# 6. WEAPON SYSTEM



## 6.1 Mouse Aim

Weapon lấy vị trí chuột (world space) để xác định hướng xoay, súng tự bắn liên tục theo Fire Rate, không cần click.

Weapon
    ↓
Get Mouse World Position (Camera.ScreenToWorldPoint)
    ↓
Rotate Toward Mouse Direction
    ↓
Auto Fire (theo Fire Rate)

## 6.2 Weapon Level

Weapon có 4 level.

Ví dụ:

| Level | Damage | Fire Rate | Bullet Count |
|---|---:|---:|---:|
| 1 | 10 | 1.0 shot/s | 1 |
| 2 | 15 | 1.2 shot/s | 1 |
| 3 | 20 | 1.5 shot/s | 2 |
| 4 | 30 | 2.0 shot/s | 2 |

Không dùng Bullet Penetration trong phiên bản đầu.

## 6.3 Weapon Upgrade

Weapon nâng cấp theo tổng số enemy đã tiêu diệt.

Ví dụ:

| Weapon Level | Kill yêu cầu |
|---|---:|
| Level 1 | 0 |
| Level 2 | 10 |
| Level 3 | 25 |
| Level 4 | 45 |

Có thể chỉnh số liệu sau khi playtest.

---

# 7. ENEMY SYSTEM

## 7.1 Normal Enemy

- HP trung bình
- Speed trung bình
- Damage trung bình

## 7.2 Fast Enemy

- HP thấp
- Speed cao
- Damage thấp

## 7.3 Tank Enemy

- HP cao
- Speed thấp
- Damage cao

Ba loại enemy này đủ để tạo sự khác biệt gameplay mà không làm AI quá phức tạp.

---

# 8. ENEMY SPAWNER

Enemy không spawn ngay trên màn hình hoặc sát Player.

Enemy nên xuất hiện ở khu vực bên ngoài Camera và chạy vào Player.

Spawner dựa trên:

```text
Spawn Interval
Max Enemies Alive
Enemy Type
Wave Difficulty
```

Không sử dụng `enemyCount` làm giới hạn chính.

Ví dụ:

```text
Wave Duration = 60s
Spawn Interval = 1.2s
Max Enemies Alive = 25
```

Logic:

```text
Nếu số enemy hiện tại < Max Enemies Alive
    → Spawn

Nếu đã đạt Max Enemies Alive
    → Chờ enemy chết rồi tiếp tục spawn
```

Cách này tránh số lượng quái tăng mất kiểm soát.

---

# 9. WAVE SYSTEM

Thay vì xây dựng 9 Level độc lập, game sử dụng 9 Wave trong cùng một Gameplay Scene.

## 9.1 Cấu trúc Wave

```text
Wave 1–3
Forest Theme

Wave 4–6
Desert Theme

Wave 7–9
Dungeon Theme
```

Theme chủ yếu thay đổi:

- Background
- Tilemap
- Màu sắc
- Decoration

Gameplay mechanics không thay đổi.

## 9.2 Độ khó

Độ khó tăng chủ yếu bằng:

1. Spawn Interval giảm.
2. Max Enemy Alive tăng.
3. HP enemy tăng nhẹ.
4. Tỉ lệ xuất hiện Fast/Tank Enemy tăng.

Không tăng quá mạnh Enemy Speed.

Ví dụ:

| Wave | Spawn Interval | HP Multiplier | Enemy |
|---|---:|---:|---|
| 1 | 1.5s | 1.0 | Normal |
| 2 | 1.4s | 1.0 | Normal |
| 3 | 1.3s | 1.1 | Normal + Fast |
| 4 | 1.2s | 1.1 | Normal + Fast |
| 5 | 1.1s | 1.2 | Normal + Fast + Tank |
| 6 | 1.0s | 1.2 | Mixed |
| 7 | 0.9s | 1.3 | Mixed |
| 8 | 0.8s | 1.4 | Mixed |
| 9 | 0.7s | 1.5 | Mixed |

Các thông số trên chỉ là giá trị ban đầu và cần cân bằng lại khi playtest.

---

# 10. ITEM DROP SYSTEM

Enemy chết có một xác suất nhỏ rơi item.

Ví dụ:

```text
Drop Chance = 20%
```

Nếu được phép drop thì random loại item.

| Item | Tỉ lệ khi đã drop | Hiệu ứng |
|---|---:|---|
| Health | 50% | +20 HP |
| Speed Boost | 25% | +20% Speed trong 5 giây |
| Damage Boost | 25% | +30% Damage trong 5 giây |

Không dùng item tăng Kill Progress trong phiên bản chính.

---

# 11. SCRIPTABLEOBJECT

ScriptableObject được sử dụng cho các dữ liệu cần chỉnh sửa thường xuyên trong Inspector.

## 11.1 WaveData

Có thể chứa:

```text
waveIndex
duration
spawnInterval
maxEnemiesAlive
enemyHPModifier
normalEnemyWeight
fastEnemyWeight
tankEnemyWeight
mapTheme
```

## 11.2 WeaponLevelData

Có thể chứa:

```text
weaponLevel
damage
fireRate
bulletCount
killRequired
```

## 11.3 EnemyData

Có thể chứa:

```text
enemyName
maxHP
movementSpeed
contactDamage
enemyPrefab
```

Không cần biến tất cả dữ liệu trong game thành ScriptableObject.

---

# 12. KIẾN TRÚC CODE

## 12.1 Core

### GameManager

Quản lý:

- Game State
- Playing
- Pause
- Game Over
- Victory
- Score / Kill Count tổng

### WaveManager

Quản lý:

- Wave hiện tại
- Timer
- Load WaveData
- Chuyển Wave
- Kết thúc game sau Wave cuối

### UIManager

Quản lý:

- HP UI
- Timer
- Wave
- Kill Count
- Weapon Level
- Pause UI
- Game Over UI
- Victory UI

---

## 12.2 Player

### PlayerController

- Nhận input.
- Di chuyển Player.

### PlayerHealth

- HP.
- Receive Damage.
- Heal.
- Damage Cooldown.
- Game Over khi HP <= 0.

---

## 12.3 Weapon

### WeaponController

- Lấy hướng chuột (world position) để xoay súng.
- Xoay súng.
- Auto Fire.
- Spawn Bullet.

### Bullet

- Di chuyển.
- Collision.
- Gây damage.
- Destroy sau khi hit hoặc hết lifetime.

### WeaponUpgradeManager

- Theo dõi Kill Count.
- Kiểm tra threshold.
- Nâng Weapon Level.

### WeaponLevelData

- Data từng level súng.

---

## 12.4 Enemy

### EnemyController

- Di chuyển về Player.

### EnemyHealth

- HP.
- Nhận damage.
- Death.
- Gửi thông báo enemy bị tiêu diệt.

### EnemySpawner

- Spawn enemy.
- Kiểm soát Max Enemies Alive.
- Chọn loại enemy dựa trên WaveData.

### EnemyData

- Dữ liệu enemy.

---

## 12.5 Item

### ItemDropManager

- Kiểm tra Drop Chance.
- Random loại item.
- Spawn Item Prefab.

### PickupItem

Dùng enum đơn giản:

```csharp
public enum ItemType
{
    Health,
    Speed,
    Damage
}
```

Không cần tạo nhiều subclass Item ở phiên bản đầu.

---

# 13. GIAO TIẾP GIỮA CÁC HỆ THỐNG

EnemyHealth không nên trực tiếp điều khiển tất cả hệ thống khác.

Khi enemy chết:

```text
EnemyHealth
    ↓
EnemyKilled Event
    ↓
--------------------------------
|              |               |
GameManager    UIManager       WeaponUpgradeManager
Kill Count     Update UI       Upgrade Progress
```

Có thể sử dụng C# Event.

Ví dụ ý tưởng:

```csharp
public static event Action OnEnemyKilled;
```

Phần này có thể triển khai sau khi gameplay cơ bản đã chạy.

Nếu event gây khó khăn ở giai đoạn đầu, có thể dùng cách gọi trực tiếp đơn giản trước rồi refactor sau.

---

# 14. CẤU TRÚC SCENE

Ưu tiên chỉ dùng:

```text
MainMenu Scene
Gameplay Scene
```

Không tạo 9 Gameplay Scene khác nhau.

Gameplay Scene dùng WaveData để thay đổi dữ liệu từng Wave.

## Gameplay Scene

Ví dụ hierarchy:

```text
Gameplay
│
├── Managers
│   ├── GameManager
│   ├── WaveManager
│   ├── EnemySpawner
│   └── UIManager
│
├── Player
│   └── Weapon
│
├── Map
│
├── Enemies
│
├── Projectiles
│
├── Items
│
└── Canvas
```

---

# 15. GAME FLOW

## Main Menu

```text
GAME TITLE

[PLAY]
[HOW TO PLAY]
[EXIT]
```

## Playing

```text
Wave bắt đầu
    ↓
Enemy spawn
    ↓
Player chiến đấu
    ↓
Timer = 0
```

## Wave Complete

```text
WAVE COMPLETE

Wave X Completed
```

Hiển thị khoảng 1–2 giây rồi sang Wave tiếp.

## Pause

```text
PAUSED

[RESUME]
[RESTART]
[MAIN MENU]
```

## Game Over

```text
GAME OVER

Wave Reached: X
Kills: XX

[RETRY]
[MAIN MENU]
```

## Victory

```text
VICTORY

Total Kills: XXX

[PLAY AGAIN]
[MAIN MENU]
```

---

# 16. LỘ TRÌNH PHÁT TRIỂN 8 TUẦN

## Tuần 1 - Unity Fundamentals

Học:

- Unity Editor
- Scene
- GameObject
- Component
- Transform
- Prefab
- SpriteRenderer
- Rigidbody2D
- Collider2D
- Inspector
- MonoBehaviour
- Start()
- Update()

Kết quả bắt buộc:

```text
Player xuất hiện trên map
Player di chuyển bằng WASD
Camera hiển thị đúng gameplay
```

---

## Tuần 2 - Core Combat

Làm:

- Enemy follow Player.
- Weapon  target follow mouse.
- Weapon auto fire.
- Bullet.
- Enemy HP.
- Enemy death.

Kết quả:

```text
Player chạy
    ↓
Enemy đuổi
    ↓
Weapon bắn
    ↓
Enemy nhận damage
    ↓
Enemy chết
```

Đây là milestone quan trọng nhất.

---

## Tuần 3 - Survival Loop

Làm:

- EnemySpawner.
- PlayerHealth.
- Contact Damage.
- Damage Cooldown.
- Timer.
- Game Over.
- Wave Complete cơ bản.

Kết quả:

```text
Spawn
↓
Fight
↓
Survive
↓
Win / Lose
```

Đây là MVP đầu tiên.

---

## Tuần 4 - Wave System

Làm:

- WaveData.
- WaveManager.
- Wave Transition.
- Difficulty Scaling.

Chỉ tạo trước:

```text
Wave 1
Wave 2
Wave 3
```

Chưa tạo đủ 9 Wave.

---

## Tuần 5 - Weapon Upgrade

Làm:

- Kill Count.
- Weapon Level.
- WeaponUpgradeManager.
- WeaponLevelData.
- Weapon Level UI.

Ví dụ:

```text
Weapon Lv.2
Kills: 17 / 25
```

---

## Tuần 6 - Item System

Làm:

- Item Drop.
- Health Pickup.
- Speed Boost.
- Damage Boost.
- Drop Chance.

---

## Tuần 7 - Content

Sau khi hệ thống đã ổn:

- Tạo đủ Wave 1–9.
- Tạo 3 Map Theme.
- Thêm Fast Enemy.
- Thêm Tank Enemy.
- Cân bằng Spawn Rate.
- Cân bằng HP/Damage.

---

## Tuần 8 - Polish & Build

Làm:

- UI hoàn chỉnh.
- Sound.
- Particle.
- Animation đơn giản.
- Pause.
- Main Menu.
- Game Over.
- Victory.
- Bug fixing.
- Build Windows `.exe`.
- Record video demo.
- Chuẩn bị báo cáo.
- Chuẩn bị slide bảo vệ.

---

# 17. MILESTONE KIỂM SOÁT SCOPE

## Milestone 1 - Prototype

Phải có:

```text
✓ Player Movement
✓ Enemy Follow
✓ Auto Aim
✓ Auto Shoot
✓ Bullet Damage
✓ Enemy Death
```

Nếu chưa đủ thì không làm Item, UI đẹp hoặc Map mới.

---

## Milestone 2 - MVP

Phải có:

```text
✓ Enemy Spawn
✓ Player HP
✓ Contact Damage
✓ Damage Cooldown
✓ Timer
✓ Game Over
✓ Wave Transition
✓ 3 Wave
```

Khi đạt milestone này, game đã có thể chơi từ đầu đến cuối ở mức cơ bản.

---

## Milestone 3 - Feature Complete

Thêm:

```text
✓ Weapon Upgrade
✓ Item Drop
✓ 3 Enemy Types
✓ 9 Wave
✓ 3 Map Themes
✓ Main Menu
✓ Pause
✓ Victory
```

---

## Milestone 4 - Polish

Nếu còn thời gian:

```text
✓ Sound Effect
✓ Background Music
✓ Particle
✓ Hit Effect
✓ Screen Shake nhẹ
✓ Animation
✓ Object Pooling
✓ Better Enemy Spawn
```

---

# 18. TÍNH NĂNG NÂNG CAO - CHỈ LÀM KHI GAME ĐÃ HOÀN THIỆN

## Priority 1 - Object Pooling

Chỉ thêm nếu game bị giảm FPS do quá nhiều:

- Bullet
- Enemy
- Particle

Phiên bản đầu có thể sử dụng:

```csharp
Instantiate();
Destroy();
```

Sau khi game chạy ổn mới cân nhắc Object Pool.

---

## Priority 2 - Event System

Có thể refactor các hệ thống để sử dụng C# Event:

```text
EnemyKilled
PlayerDamaged
WaveStarted
WaveCompleted
WeaponUpgraded
```

Điểm cộng khi trình bày kiến trúc phần mềm.

---

## Priority 3 - Juice / Game Feel

Có thể thêm:

- Screen shake.
- Hit flash.
- Enemy knockback.
- Particle khi enemy chết.
- Floating damage number.
- Camera effect.
- Sound feedback.

Không ảnh hưởng core gameplay nhưng làm game cảm giác hoàn thiện hơn.

---

## Priority 4 - Boss

Boss chỉ được thêm nếu:

```text
MVP hoàn thiện
+
9 Wave chạy ổn
+
Không còn bug nghiêm trọng
+
Còn thời gian
```

Nếu thêm boss, chỉ cần một boss đơn giản ở Wave 9.

Không bắt buộc.

---

# 19. RỦI RO CHÍNH

## 19.1 Scope quá lớn

Không thêm tính năng mới chỉ vì thấy tutorial hoặc game khác có.

Luôn ưu tiên:

```text
Game chạy hoàn chỉnh
>
Game có nhiều chức năng nhưng không hoàn thiện
```

## 19.2 Làm Asset trước Logic

Không dành quá nhiều thời gian cho:

- Art.
- Animation.
- Map.
- UI đẹp.

trước khi core gameplay chạy được.

## 19.3 Hardcode quá nhiều

Các thông số thường xuyên thay đổi nên đưa ra Inspector hoặc ScriptableObject:

- HP.
- Damage.
- Speed.
- Spawn Interval.
- Wave Duration.
- Drop Chance.

## 19.4 Quá nhiều Enemy

Sử dụng:

```text
Max Enemies Alive
```

để tránh game spawn không giới hạn.

---

# 20. NỘI DUNG NÊN TRÌNH BÀY KHI BẢO VỆ

Các phần nên nhấn mạnh:

## 20.1 Kiến trúc hệ thống

```text
WaveManager
    ↓
EnemySpawner
    ↓
Enemy
    ↓
EnemyHealth
    ↓
Kill Event
    ↓
WeaponUpgrade / UI / Score
```

## 20.2 ScriptableObject

Giải thích:

> Các thông số gameplay được tách khỏi code để dễ chỉnh sửa, cân bằng và tái sử dụng.

## 20.3 Difficulty Scaling

Giải thích cách WaveData thay đổi:

- Spawn Rate.
- Max Enemy Alive.
- Enemy HP.
- Enemy Composition.

## 20.4 Weighted Random

Dùng trong Item Drop hoặc Enemy Type Selection.

## 20.5 Mouse Aim Algorithm
Giải thích cách Weapon lấy Input.mousePosition → Camera.ScreenToWorldPoint để xoay hướng bắn, độc lập với auto-fire.

## 20.6 Damage Cooldown

Giải thích vì sao player không nhận damage mỗi frame khi collider tiếp xúc enemy.

---

# 21. TIÊU CHÍ HOÀN THÀNH ĐỒ ÁN

Đồ án được xem là hoàn chỉnh khi người chơi có thể:

```text
Mở Game
    ↓
Main Menu
    ↓
Play
    ↓
Wave 1
    ↓
...
    ↓
Wave 9
    ↓
Victory
```

hoặc:

```text
Player HP = 0
    ↓
Game Over
    ↓
Retry / Main Menu
```

Game cần chạy ổn định từ đầu đến cuối mà không cần thao tác trong Unity Editor.

---

# 22. NGUYÊN TẮC PHÁT TRIỂN

Thứ tự ưu tiên:

```text
1. Gameplay chạy được
2. Gameplay chạy ổn định
3. Gameplay cân bằng
4. UI rõ ràng
5. Âm thanh / hiệu ứng
6. Tối ưu
7. Tính năng nâng cao
```

Không đảo thứ tự này.

---

# 23. PHẠM VI CUỐI CÙNG

```text
GAME
2D Top-down Survival Auto Shooter

PLAYER
- Movement
- HP
- Damage Cooldown

WEAPON
- 1 Weapon
- Mouse Aim
- Auto Fire
- 4 Weapon Levels

ENEMY
- Normal
- Fast
- Tank

PROGRESSION
- 9 Waves
- 3 Map Themes
- Difficulty Scaling

ITEM
- Heal
- Speed Boost
- Damage Boost

UI
- HP
- Timer
- Wave
- Kill Count
- Weapon Level

GAME FLOW
- Main Menu
- Playing
- Pause
- Wave Complete
- Game Over
- Victory
```

Đây là phạm vi chính thức nên khóa cho đồ án.

Các chức năng ngoài danh sách này được xem là **Optional Feature** và chỉ triển khai sau khi toàn bộ game đã hoàn thiện.

---

# 24. NHẬT KÝ TIẾN ĐỘ VÀ MỤC TIÊU TIẾP THEO

## Ngày 18/09/2026 — Survival Loop và MVP 3 Wave

> Đây là nhật ký trước đợt sửa combat. Các lỗi/thông số bên dưới được giữ để truy vết; cấu hình và kết quả mới nhất ở mục cập nhật cuối ngày phía dưới.

### Công việc đã thực hiện

- Tiếp tục phát triển trên nền combat đã có: Player di chuyển, quái đuổi, súng ngắm theo chuột và auto-fire; dùng `GameScene` làm scene gameplay chính.
- Bổ sung `PlayerHealth`: HP tối đa 100, nhận sát thương tiếp xúc và miễn nhiễm 0.75 giây sau mỗi lần bị đánh; nhiều quái dùng chung cooldown của Player.
- Bổ sung `EnemyContactDamage` vào Enemy prefab, sát thương tiếp xúc mặc định 10; Player prefab đã có component quản lý HP.
- Sửa `EnemyHealth` để HP không xuống âm và không xử lý nhận damage/chết lặp lại khi quái đã hết HP.
- Đã thử cơ chế hồi sinh đầy HP phục vụ test, sau đó bỏ khỏi vòng chơi chính để giữ luật một mạng: HP về 0 → Game Over.
- Bổ sung `GameManager` và `WaveManager`: timer, trạng thái Playing/Game Over/Wave Complete; khi lượt kết thúc, dừng spawn, điều khiển, bắn và physics.
- Mở rộng thành 3 wave trong cùng một scene; hết Wave 1/2 chuyển ngay sang wave tiếp, hết Wave 3 hiện `ALL WAVES COMPLETE`.
- Theo yêu cầu mới, giảm thời lượng từ 60 xuống **30 giây mỗi wave**. Đây là thời lượng hiện tại, thay cho khoảng 45–60 giây trong bản kế hoạch ban đầu.
- Tăng áp lực spawn: khoảng cách giữa các lần spawn lần lượt 1.5 / 1.395 / 1.305 giây; giới hạn quái đang sống 10 / 13 / 16.
- Thêm UI prototype hiển thị HP, timer, wave và kết quả; nút `PLAY AGAIN` tải lại scene để bắt đầu lượt mới.
- Cấu hình Build Settings dùng `GameScene`; mục `SampleScene` không tồn tại đã được tắt.
- Rà code, scene, prefab và git diff; tạo bộ nhớ dự án trong `AGENTS.md` và `docs/` để phiên làm việc sau tiếp tục được.

### Kết quả kiểm tra và trạng thái milestone

- Đã biên dịch toàn bộ script bằng Roslyn với Unity assemblies: không có lỗi C#. Các cảnh báo biến Inspector chưa gán trong code không thay thế việc kiểm tra tham chiếu scene.
- Người dùng báo đã hoàn thành thao tác trong Unity. Agent chưa trực tiếp xác nhận một lượt Play Mode đầy đủ hoặc bản build Windows.
- Lần mở Unity batchmode để kiểm tra bị chặn do project đang mở ở một Unity instance khác.
- **Tuần 3:** các chức năng Survival Loop đã được triển khai.
- **Milestone 2:** đã có các thành phần MVP 3 wave, nhưng còn lỗi difficulty và cần kiểm thử đầu-cuối trước khi chốt hoàn thành ổn định.
- **Tuần 4:** mới có WaveManager và chuyển wave dạng prototype; chưa có WaveData và màn hình chuyển wave 1–2 giây.

### Sai sót và phần còn thiếu được ghi nhận

- **Lỗi đã xác nhận:** hai hàm tính hệ số HP/tốc độ trong `EnemySpawner` switch theo `currentWave` nhưng dùng case `10` và `13`, thay vì `1` và `2`. Vì vậy quái mới ở cả Wave 1/2/3 đều có HP ×1.2 và tốc độ ×1.1; chưa tăng riêng từng wave như dự định. Spawn interval và giới hạn số quái vẫn tăng đúng. Lần review trước đã bỏ sót lỗi này.
- `WaveManager` chưa gán EnemySpawner trong Inspector; code có tự tìm để chạy, nhưng nên gán rõ và lưu scene.
- Wave 1/2 chuyển ngay, chưa hiện `WAVE COMPLETE` trong 1–2 giây theo thiết kế.
- Bullet prefab có hai trigger collider; cần xác nhận một viên đạn chỉ gây một lần damage, không kết luận lỗi này đã xảy ra khi chưa test.
- Quái/đạn còn sống và HP Player được giữ qua wave; chưa chốt chính sách xử lý quái khi thêm thời gian nghỉ giữa wave.
- Player chưa bị giới hạn trong arena; chính sách camera/ranh giới map cần xác định sau.

### Mục tiêu phiên tiếp theo — ưu tiên ổn định MVP

1. Sửa case difficulty theo Wave 1/2/3 và kiểm tra quái mới: hệ số tốc độ ×1 / ×1.05 / ×1.1; HP ×1 / ×1.1 / ×1.2. Với prefab gốc, HP dự kiến 30 / 33 / 36 và tốc độ 2 / 2.1 / 2.2.
2. Gán EnemySpawner vào component WaveManager, lưu `GameScene` và kiểm tra lại các tham chiếu không bị None.
3. Test đủ Wave 1 → 2 → 3, từng wave 30 giây; xác nhận cap 10 / 13 / 16, quái chết thì được spawn bù và chỉ spawn ngoài camera.
4. Test contact damage, cooldown khi nhiều quái chạm, HP về 0 chỉ Game Over một lần, đạn gây đúng 10 damage mỗi lần hit và HP không âm.
5. Test `PLAY AGAIN` sau cả Game Over lẫn hoàn thành 3 wave: HP đầy, timer 30, quay về Wave 1 và combat hoạt động lại. Ghi kết quả thực tế, kiểm tra Console không có lỗi đỏ.

### Mục tiêu sau khi MVP ổn định — Tuần 4

1. Tạo `WaveData` bằng ScriptableObject để chỉnh thời lượng, spawn interval, số quái tối đa và hệ số HP trong Inspector, thay cho difficulty hardcode.
2. Vẫn giữ 3 wave và 30 giây mỗi wave ở bước này; chưa mở rộng ngay lên 9 wave.
3. Thêm thông báo `WAVE COMPLETE` trong 1–2 giây rồi tự chuyển wave; chốt cách xử lý quái/đạn còn lại trước khi triển khai.
4. Cân bằng thông số bằng playtest. Chỉ sang nâng cấp súng/Kill Count của Tuần 5 sau khi hệ thống wave ổn định.

Chi tiết bàn giao và vấn đề cập nhật gần nhất: [STATE](docs/STATE.md), [DECISIONS](docs/DECISIONS.md), [ARCHITECTURE](docs/ARCHITECTURE.md), [GAME_DESIGN](docs/GAME_DESIGN.md).

## Cập nhật 18/09/2026 — Sửa combat, độ khó và camera

### Đã hoàn thành

- Thay switch sai bằng danh sách cấu hình riêng từng wave trong Inspector của WaveManager; không còn nhân chồng HP/tốc độ.
- Wave 1: **30 giây**, tối đa **10 quái đang sống**, mỗi quái **10 HP**, spawn mỗi **1.5 giây**, tốc chạy **2 units/s**.
- Wave 2: **30 giây**, tối đa **15 quái đang sống**, mỗi quái **20 HP**, spawn mỗi **1 giây**, tốc chạy **2.5 units/s**.
- Wave 3: **45 giây**, tối đa **30 quái đang sống**, mỗi quái **30 HP**, spawn mỗi **0.6 giây**, tốc chạy **3 units/s**.
- Đây là giới hạn quái sống đồng thời, không phải tổng số quái cả wave. Quái bị giết sẽ được spawn bù đến khi hết giờ.
- Gán rõ EnemySpawner trong WaveManager và lưu GameScene; áp dụng thông số wave cả cho quái đặt sẵn trong scene.
- Thêm WAVE COMPLETE **2 giây** sau Wave 1/2; dừng combat, dọn quái/đạn cũ, giữ HP và vị trí Player rồi tự chuyển wave.
- Kết thúc Wave 3 vào Victory / ALL WAVES COMPLETE; Player chết vào Game Over; cả hai có PLAY AGAIN.
- Chặn một viên đạn gây damage nhiều lần. Tắt BoxCollider2D thừa trong Bullet prefab, giữ CircleCollider2D; không xóa collider cũ để dễ khôi phục.
- Thêm CameraFollow, gán Player, giữ Z của camera; camera theo Player theo X/Y. Spawn theo camera và cách Player tối thiểu 3 đơn vị.
- Chưa đặt tường/giới hạn arena vì chưa có kích thước map được thiết kế; lựa chọn hiện tại là Player di chuyển tự do với camera đi theo.

### Đã kiểm tra thực tế

- Biên dịch C# thành công; Unity đã import/compile và lưu scene/prefab bằng Editor API.
- Chạy regression trong **Unity Play Mode thật**: toàn bộ kiểm tra PASS, không có lỗi runtime trong bài test.
- Kiểm tra cap, HP, tốc độ, cấu hình thời gian/tần suất của cả 3 wave; spawn ngoài camera, spawn bù khi quái chết.
- Thử va chạm Physics2D khi bật lại cả hai collider của đạn: chỉ mất đúng 10 HP; gọi lặp callback cũng không nhân damage; HP không âm.
- Kiểm tra camera theo Player, cooldown HP, timer hết -> nghỉ 2 giây -> wave tiếp, dọn quái/đạn và giữ HP.
- Kiểm tra Victory, Game Over và hàm chơi lại sau cả hai kết quả: về Wave 1, HP đầy, timer/điều khiển/timeScale được khôi phục.
- Menu chạy lại bài test: **Tools → Brotato → Run Combat Regression** (đang ở Edit Mode). Kết quả cục bộ: Temp/CombatRegression.txt.
- Giới hạn: test chủ động rút ngắn timer và ép lịch spawn để kiểm tra nhanh các nhánh; **chưa phải lượt chơi cân bằng đủ 30/30/45 giây**, chưa test bản Windows build.

### Mục tiêu tiếp theo

1. Chơi thử đủ thời lượng 30/30/45 giây để đánh giá Wave 2/3 có quá khó không; thử vừa di chuyển vừa ngắm/bắn với camera đi theo.
2. Build Windows và test khởi động/chơi lại ngoài Editor.
3. Khi tiếp tục Tuần 4: chuyển dữ liệu inline hiện có sang WaveData ScriptableObject nếu cần tái sử dụng; giữ thông số mới và cơ chế nghỉ/dọn quái đã chốt.
4. Sau khi ổn định mới làm Kill Count và nâng cấp súng; không tự mở rộng ngay lên 9 wave.

**Trạng thái:** các lỗi combat được yêu cầu đã sửa và qua Play Mode regression. MVP vẫn cần một lượt playtest đủ thời lượng và kiểm tra build trước khi chốt demo.
