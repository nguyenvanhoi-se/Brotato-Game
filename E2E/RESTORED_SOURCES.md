# Nguồn đã khôi phục — không có implementation giả

## WaveSettings

- Nguồn thật: `Assets/Wave/WaveManager.cs:4–21` tại upstream commit
  `1c8bc664c1433b9ce8f47fb4c1ca6c045ac042df` (04/10/2026).
- Git blob `ef6fead774e3b2d229f7fc8f6e22c5fe1985c1b1`, vẫn tồn tại trong remote main
  `fcedde054ed588de5f2c578dde6d64191a558e67`.
- Đã tách nguyên class `[Serializable] WaveSettings` vào
  `Assets/Scripts/Wave/WaveSettings.cs`. Giữ nguyên 5 fields/defaults/Min attributes
  và constructor 5 tham số; không tạo constructor rỗng hoặc dữ liệu thay thế.
- File dữ liệu mới có GUID riêng `73fd1c88fe964f1ea503d73b0267338f`; không chiếm GUID
  của whole WaveManager MonoBehaviour. Không có asset hiện tại tham chiếu GUID mới.
- Không kéo WaveManager vào scene/project: nó còn cần GameManager không có trong
  checkout; thêm cả hệ thống đó nằm ngoài phạm vi sửa compile.

## PlayerHealth và PlayerController

- Nguồn thực tế được tìm thấy ở
  `D:/LT game/CKGame/game_brotato/Assets/Scripts/Player/`.
- Repo backup remote: `https://github.com/nguyenquanghuy12052005/game_brotato.git`,
  HEAD quan sát được `2cd4e1693f336686431232cb1117ff08b6c532de`. Hai file source này
  tồn tại trong working tree backup; không tuyên bố chúng được commit trong HEAD đó.
- `PlayerHealth.cs` được copy nguyên byte cùng `.meta` vào đường dẫn tương ứng của
  project hiện tại. SHA256 source:
  `3fc34a08c731562b1d08cdc5515d0f596135676bbe44b61bc06185c52f7fbe15`.
  GUID giữ nguyên `a9ea7ccabe7acc746ae5fb6930c54e18`.
- Contract thật: HP100, cooldown .75s, bỏ qua nonpositive damage/dead/disabled,
  clamp0 và event `Died`; dependency chỉ System + UnityEngine.
- `PlayerController.cs` cũng copy nguyên byte cùng `.meta`. SHA256 source:
  `efdfec005f48ffc74a10017f15fa6e23fa1c9dab53edc589764c6d9e091b6f5f`.
  GUID `62d4b7f376264d24f9fe7489edfe02e0` khớp chính xác script reference trên
  Player prefab hiện tại. Dependency Input System đã có trong Packages.
- Controller giữ nguyên WASD, normalize movement, Rigidbody2D/FixedUpdate và
  playerVisual rotation. Không sửa balance, scene, prefab, package hoặc InputSettings.

## Lỗi hạ tầng được sửa

- Trước phục hồi: compiler xác nhận WaveSettings CS0246.
- Sau phục hồi WaveSettings: compiler xác nhận PlayerHealth CS0246.
- Sau phục hồi PlayerHealth: compile qua nhưng không tìm thấy E2EBuild vì define
  BROTATO_E2E chưa áp dụng vào Editor assembly. Cờ extraScriptingDefines cho player
  không thay thế việc cấu hình define trước khi compile Editor.
- `E2EConfigure.Configure` chạy ở lượt Editor thứ nhất, chỉ trong clone có marker,
  giữ defines cũ và lưu ALTTESTER/BROTATO_E2E; lượt thứ hai mới compile/build.
- Hook SetWave dùng constructor gốc `new WaveSettings(30f,cap,health,interval,speed)`.
  Đã bỏ giả định Activator.CreateInstance với constructor rỗng.
- Chỉ hoàn thiện implementation hai case Contact đã có SIM-027/028. Tổng số ID vẫn32;
  runtime contact không được coi là PASS trước khi có thực thi qua server.

## Bảo toàn Git

- Branch trước: codex/13_10; branch đích: **20_10**, tạo trực tiếp từ **main local**
  `e5f0fc595701402d9277c33faf1dfba35f0d87dc`.
- Snapshot ZIP và SHA256 manifest tại `.e2e/safety/20261020T095238735606Z/`;
  130 file trước chuyển nhánh đã được đối chiếu giữ nguyên từng byte.
- Recovery stash được giữ lại; không drop, không reset, không force push, không commit/push.
- Các file hướng dẫn/ảnh vốn có trong commit của codex/13_10 được giữ nguyên trong
  working tree mới nên một số hiện xuất hiện untracked. Chúng không phải thay đổi
  gameplay mới do nhiệm vụ E2E tạo ra.
- Đã xem diff remote main: nó thêm WaveManager phụ thuộc GameManager và đổi
  EnemyHealth với `enemiesParent` chưa khai báo. Chưa tích hợp các thay đổi đó vào
  main local; tránh ghi đè source hiện tại hoặc kéo thêm gameplay ngoài phạm vi.
