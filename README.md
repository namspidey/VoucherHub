# VoucherHub

Kiến trúc BE/FE sau refactor, quan hệ giữa các lớp và luồng dữ liệu: [Architecture guide](docs/architecture-guide.md).

Ứng dụng ASP.NET Core .NET 10 dùng PostgreSQL để quản lý điểm và đổi voucher trong flash sale. Source gồm Domain, Application, Infrastructure và Api. Tám bảng nghiệp vụ đã được mô tả tại [kế hoạch backend](docs/backend-completion-plan.md); API ở [tài liệu API](docs/api-reference.md).

## Chạy local

Cần .NET SDK 10, Docker Desktop và PowerShell. Tại `D:\VoucherHub`:

Nếu clone mới: copy `compose.example.yaml` thành `compose.yaml`, thay `replace-with-local-password` bằng mật khẩu local riêng và cấu hình User Secrets với cùng mật khẩu. `compose.yaml` chứa cấu hình riêng nên không commit; máy đã có file này không cần thay hoặc tạo lại volume.

```powershell
docker compose up -d
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project backend/src/VoucherHub.Api --launch-profile http
```

API mặc định `http://localhost:5242`, Mailpit `http://localhost:8025`. User Secrets của project Api phải có `ConnectionStrings:DefaultConnection` và `Jwt:SigningKey` (ít nhất 32 byte). Không commit secret thật.

Để tạo cấu hình local mới, dùng `dotnet user-secrets set` với `--project backend/src/VoucherHub.Api`. Connection string trỏ database `voucherhub` trên `127.0.0.1:5432`, user từ `compose.yaml`; lấy password local từ Compose. Có thể thay bằng environment variables `ConnectionStrings__DefaultConnection` và `Jwt__SigningKey`.

Kiểm tra DB/migration trước khi áp dụng thay đổi schema:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef migrations list --project backend/src/VoucherHub.Infrastructure --startup-project backend/src/VoucherHub.Api
```

Chỉ chạy `dotnet ef database update` sau khi xem migration mới và xác nhận đúng database đích. Không dùng `docker compose down -v` cho lần khởi động lại thông thường.
[Hướng dẫn migration và backup/restore](docs/database-operations.md) có cách diễn tập trên DB khác để không đè dữ liệu local.

## Kiểm tra

```powershell
dotnet build VoucherHub.slnx -c Release
Invoke-RestMethod http://localhost:5242/health/live
Invoke-RestMethod http://localhost:5242/health/ready
```

`scripts/test-create-campaign.ps1` và `scripts/test-backend-core.ps1` cần API đang chạy; script core mặc định dùng cổng 5108, có thể truyền `-BaseUrl http://localhost:5242`. Script tạo email `example.test` ngẫu nhiên rồi xóa tài khoản/campaign do chính lần chạy đó tạo. `scripts/test-backend-isolated.ps1` tự tạo database PostgreSQL tạm, áp dụng migrations, chạy API Release ở cổng 5118 và test cả lỗi giữa transaction; cuối cùng dừng API và xóa database tạm. Build Release trước khi chạy script này. Không chạy các script test chống lại production.

Import [Postman collection](docs/VoucherHub.postman_collection.json) và [environment template](docs/VoucherHub.postman_environment.json), chọn environment, điền email/password test. Script sau login tự lưu `access_token` hoặc `admin_token`; request trong folder tương ứng tự dùng token. Với redeem, `request_key` được sinh lần đầu; giữ key để retry, xóa biến để bắt đầu lượt đổi mới.

Tạo admin local: đăng ký và verify một email riêng, chạy `scripts/bootstrap-admin.sql` theo hướng dẫn trong [API reference](docs/api-reference.md), rồi login lại để token có role ADMIN. Không có public API cấp role ADMIN.

## Cấu hình trước khi deploy

| Biến/section | Mục đích |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL bền vững, TLS/quyền theo môi trường |
| `Jwt__SigningKey`, `Jwt__Issuer`, `Jwt__Audience` | Ký và xác thực token |
| `Registration__VerificationPageUrl` | Trang frontend nhận token xác thực |
| `Smtp__Host`, `Smtp__Port`, `Smtp__Username`, `Smtp__Password`, `Smtp__FromEmail`, `Smtp__EnableSsl` | Provider gửi email thật |
| `Cors__AllowedOrigins__0` | Origin frontend được phép gọi API |
| `Proxy__KnownProxies__0` | IP reverse proxy tin cậy để đọc `X-Forwarded-For/Proto` |
| `RateLimits__RegistrationPerMinute`, `RateLimits__LoginPerMinute`, `RateLimits__RedeemPerMinute` | Giới hạn request mỗi phút |

Dockerfile ở root tạo image API multi-stage; truyền secret lúc chạy container, không đóng vào image. `/health/live` kiểm tra process; `/health/ready` kiểm tra PostgreSQL. OpenAPI JSON chỉ map trong Development. Khi chạy sau reverse proxy, cấu hình HTTPS và `Proxy__KnownProxies__0` bằng IP proxy thực, không tin mọi địa chỉ chuyển tiếp. [Tài liệu ASP.NET Core về proxy](https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0) giải thích việc này ảnh hưởng URL scheme và IP dùng cho rate limit.

Database là nguồn quyết định đổi voucher; cache chỉ hỗ trợ đọc với TTL 2 giây cho response campaign có tồn kho. Hiện cache và rate limit ở bộ nhớ của một instance; mở rộng nhiều instance cần thiết kế invalidation/limiter dùng chung. Email gửi sau commit; nếu SMTP lỗi, pending vẫn còn và có thể resend sau cooldown. Admin grant hiện chưa có idempotency key; kiểm tra lịch sử trước khi gửi lại request không rõ kết quả.
