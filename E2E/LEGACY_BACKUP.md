# Bảo toàn trước khi gỡ công cụ cũ

Ngày27/10/2026, trước khi migrate framework:

- `.e2e/backups/pre-independent-e2e-20261027/tracked-e2e.zip`: source/spec/logic
  E2E, AGENTS.md và gitignore từ HEAD d89d3c5.
- `reports.zip`: toàn bộ historical report/log/JSON/XML/PNG bên ngoài payload build.
- `helpers.zip`: helper cũ gắn với các build/report trước đây.
- `manifest.json`: SHA256 archives ban đầu và83 file source/config gốc.
- `cleanup.json`: danh sách43 entry đã gỡ có kiểm tra ownership/path.

ZIP reports đã kiểm CRC. Bản report cũ ngoài build folder vẫn còn ở nguyên đường dẫn
để các link lịch sử tiếp tục đọc được. Những chữ/tên công cụ cũ trong report/ZIP này
là bằng chứng lịch sử, không phải dependency hoặc configuration đang hoạt động.

Đã gỡ Desktop2.4.0 bằng uninstaller chính thức exit0, Server binary/config tại
profile riêng, SDK clone,7 generated Unity clones,8 build payload folders và
Python dependencies chỉ thuộc công cụ cũ. Đã giữ pytest và dependency dùng chung.
Không nối path từ pip RECORD một cách mù quáng; các package/script đều theo whitelist.

Không xóa `.e2e/safety`, `.e2e/recovery`, venv trống, game profiles, Unity settings,
source/meta đã phục hồi hoặc lịch sử Git. Không commit/push.

Sau migration, đối chiếu lại manifest83 file để xác nhận toàn bộ source/config gốc
không đổi. Kết quả cuối và audit cleanup thực tế nằm trong VERIFICATION.md.
