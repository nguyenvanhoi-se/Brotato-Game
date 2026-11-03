# M3 — AI hiện tại, Offline Sync và Automated Tests

Phạm vi thống nhất ngày 03/11/2026: giữ SQLite đã được giảng viên chấp thuận; giữ MediaPipe + PCA/Random Forest, việc đổi sang framework AI khác được hoãn. Máy chủ đồng bộ demo dùng Python + SQLite trên máy tính, không cần tài khoản cloud hoặc thư viện Python bổ sung.

## Chạy demo

Yêu cầu: Python 3, Flutter SDK, Android Emulator đã mở. Hai lệnh bên dưới chạy từ thư mục Flutter của dự án.

Terminal 1 — máy chủ:

```powershell
cd D:\danentang\do_an_da_nen_tang\LingoSign_Agent_Skills_Bundle_Resume_Ready
.\tool\sync_server\start-server.ps1
```

Terminal 2 — app:

```powershell
cd D:\danentang\do_an_da_nen_tang\LingoSign_Agent_Skills_Bundle_Resume_Ready
.\tool\sync_server\run-app.ps1
```

Nếu máy ảo chưa mở, chạy lệnh sau trong terminal riêng và chờ màn hình chính:

```powershell
& "$env:LOCALAPPDATA\Android\Sdk\emulator\emulator.exe" -avd Medium_Phone_API_36 -camera-front webcam0 -no-snapshot-load
```

Vào **Cài đặt → Đồng bộ lịch sử** và bật công tắc. Mặc định tắt; AI, lịch sử và ảnh hoạt động bình thường khi không bật. Script tạo mã kết nối ngẫu nhiên trong `.sync-demo/token.txt` và database máy chủ `.sync-demo/server.sqlite`; cả thư mục được Git ignore. Không đưa token vào báo cáo/Git. Có thể truyền `-Flutter <flutter.bat>` hoặc `-Device <device-id>` cho script chạy app. URL `10.0.2.2` dành cho Android Emulator trên máy tính này.

Khi chạy lệnh Flutter thông thường không có `--dart-define`, app vẫn chạy offline nhưng thông báo chưa cấu hình máy chủ. Đổi cấu hình đồng bộ cần chạy lại app, hot reload không cập nhật dart-define.

## Kịch bản trình bày

1. Bật đồng bộ, nhận diện một ký hiệu tin cậy từ 60% trở lên. Kiểm tra ảnh/kết quả trong Lịch sử và số chờ đồng bộ về 0 ở Cài đặt.
2. Dừng máy chủ bằng Ctrl+C ở terminal 1. Nhận diện vài ký hiệu hoặc xóa một bản ghi. App vẫn lưu SQLite; số chờ tăng, lỗi kết nối được hiển thị.
3. Đóng/mở lại app bằng script terminal 2 trong khi máy chủ vẫn tắt. Kiểm tra lịch sử và hàng đợi còn nguyên.
4. Chạy lại script máy chủ ở terminal 1. App đang mở tự thử mỗi 5 giây, tôn trọng backoff 2, 4, 8… giây đến tối đa 5 phút. Nút Đồng bộ ngay cũng tôn trọng thời gian chờ. Chờ số hàng đợi về 0.
5. Kiểm tra bản ghi đã xóa không xuất hiện trở lại. Có thể dùng thiết bị giả lập thứ hai cùng token để kiểm tra lịch sử tải về (ảnh chỉ có trên thiết bị chụp).

Ngắt máy chủ là cách tái lập mất kết nối endpoint. Không gọi đây là kiểm chứng airplane mode trên điện thoại thật; phần đó còn cần thực hiện thủ công.

## Cách triển khai

- SQLite là nguồn dữ liệu cho UI. Ghi lịch sử/xóa và ghi outbox trong cùng transaction.
- Schema v4 thêm `next_attempt_at_utc_ms`; nâng cấp v1/v2/v3 bảo toàn dữ liệu đã có.
- Hàng đợi tồn tại qua restart; lưu retry count, lỗi và thời điểm thử lại. Tối đa 100 thao tác mỗi lượt, không chạy hai lượt đồng thời.
- Mã thao tác ổn định khi retry; máy chủ ACK sau khi commit SQLite. Replay cùng thao tác không nhân bản. Dùng lại mã với payload khác bị từ chối.
- Pull tăng dần theo cursor, tối đa 100 thay đổi/trang. Ghi cả trang và cursor trong cùng transaction; lỗi giữa trang rollback cả hai. Pull không tự tạo outbox.
- Bản ghi nhận diện là snapshot bất biến. Cùng ID dùng bản máy chủ đã nhận trước; tombstone luôn thắng upsert để tránh hồi sinh bản ghi đã xóa. Hai thiết bị xóa cùng bản ghi không chặn hàng đợi.
- Bật đồng bộ sẽ thêm lịch sử cũ vào hàng đợi; tắt ngăn các yêu cầu mới. Yêu cầu đã gửi có thể đã được máy chủ xử lý. Tắt không xóa dữ liệu đã có trên máy chủ.
- Chỉ đồng bộ ID, nhãn, mã ký hiệu, thời gian nhận diện/xóa. Không gửi ảnh, video hoặc đường dẫn ảnh. Ảnh được giữ tại thiết bị gốc; bản ghi tải về dùng ảnh trống nếu thiết bị không có ảnh.
- Auto sync chỉ hoạt động khi app đang chạy; không có dịch vụ nền sau khi hệ điều hành đóng app.

Máy chủ này là demo cho một người dùng/nhóm thiết bị cùng token, mặc định chỉ lắng nghe loopback. HTTP chỉ được cho phép ở bản Android debug để demo local. Không phải backend production, không triển khai đăng nhập/phân quyền nhiều tài khoản hoặc cloud. Bản release cần HTTPS nếu kết nối backend thực tế. Giữ nguyên database máy chủ giữa các lần demo để giữ cursor và lịch sử đồng bộ.

## Kiểm chứng tự động

```powershell
& 'D:\danentang\.tooling\flutter\bin\flutter.bat' analyze --no-pub
& 'D:\danentang\.tooling\flutter\bin\flutter.bat' test --no-pub
python tool/sync_server/test_server.py
```

- Unit/repository: offline create, opt-in, restart, backoff bền vững, reconnect, lost ACK, concurrent sync, rollback outbox, tombstone, pull không lặp, migration v3 và rollback trang lỗi.
- HTTP integration: khởi động Python server thật với DB tạm, hai SQLite client, push/pull, xác thực sai, replay, stale upsert, ngắt/kết nối lại và restart máy chủ. Dữ liệu đều là fixture, không có ảnh người dùng.
- Server tests: phân trang 105 thay đổi, restart, delete-before-upsert, idempotency, từ chối payload khác cùng mã thao tác và từ chối trường ảnh/timestamp sai.
- Widget: cấu hình thiếu, bật/tắt đồng bộ, số chờ và lỗi offline; các test camera/lịch sử/theme hiện có vẫn chạy.
- CI đã được cấu hình chạy bộ test Flutter và Python; chưa xác minh một lượt GitHub Actions từ thay đổi này.

Các test AI hiện tại kiểm tra pipeline bằng vector/fixture tổng hợp; không phải phép đo accuracy trên dữ liệu bàn tay thật. Không tạo số liệu accuracy/F1 mới. Cần tự quay demo Android để nghiệm thu thao tác thực tế; phần đổi framework AI được hoãn theo yêu cầu.
