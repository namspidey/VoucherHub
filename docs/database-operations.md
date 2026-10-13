# VoucherHub — migration và backup PostgreSQL

Áp dụng cho local Compose để diễn tập; khi có host thật phải thay database/đường dẫn/credential và kiểm tra quyền truy cập. Không dùng lệnh restore lên database đang phục vụ người dùng.

## Kiểm tra trước migration

```powershell
cd D:\VoucherHub
docker compose ps
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef migrations list --project backend/src/VoucherHub.Infrastructure --startup-project backend/src/VoucherHub.Api
dotnet ef migrations has-pending-model-changes --project backend/src/VoucherHub.Infrastructure --startup-project backend/src/VoucherHub.Api
```

Đối chiếu DB đích trong connection string, tên migration, file `Up`/`Down` và SQL sẽ chạy. API không tự chạy migration lúc startup. Khi có migration mới, tạo script SQL để review trước:

```powershell
dotnet ef migrations script --idempotent --project backend/src/VoucherHub.Infrastructure --startup-project backend/src/VoucherHub.Api --output .\migration-review.sql
```

File SQL review có thể chứa tên schema/dữ liệu nhạy cảm tùy migration; kiểm tra trước khi chia sẻ. Sau khi xác nhận backup và tác động, mới chạy `dotnet ef database update` với đúng môi trường. Không sửa migration đã áp dụng, không drop bảng hoặc xóa Docker volume để chữa lỗi migration.

## Backup local bằng custom format

Trong PowerShell, tránh redirect stream nhị phân `pg_dump -Fc` bằng `>` vì Windows PowerShell cũ có thể đổi encoding. Để `pg_dump` ghi file trong container rồi copy file đó ra host:

```powershell
New-Item -ItemType Directory -Path .\backups -Force | Out-Null
$dbContainer = docker compose ps -q postgres
docker compose exec -T postgres pg_dump -U voucherhub -Fc -f /tmp/voucherhub.backup voucherhub
docker cp "${dbContainer}:/tmp/voucherhub.backup" .\backups\voucherhub.backup
Get-Item .\backups\voucherhub.backup | Select-Object FullName,Length
```

Giữ file backup ngoài repo và bảo vệ như dữ liệu production: chứa email, password hash và mã voucher. Đặt `backups/` trong `.gitignore` khi khởi tạo Git. Backup có giá trị khi thử restore thành công, không chỉ khi file tồn tại.

## Diễn tập restore vào DB mới

Ví dụ tên DB thử nghiệm `voucherhub_restore_test`. Kiểm tra tên chưa tồn tại trước khi tạo. Các lệnh sau **không** đè `voucherhub`:

```powershell
docker compose exec -T postgres createdb -U voucherhub voucherhub_restore_test
$dbContainer = docker compose ps -q postgres
docker cp .\backups\voucherhub.backup "${dbContainer}:/tmp/voucherhub.backup"
docker compose exec -T postgres pg_restore -U voucherhub -d voucherhub_restore_test --no-owner --no-acl /tmp/voucherhub.backup
docker compose exec -T postgres psql -U voucherhub -d voucherhub_restore_test -c '\dt'
```

Chỉ xóa DB diễn tập sau khi đã xác nhận đúng tên và không có ứng dụng nào đang dùng nó. Không đưa `dropdb` vào lệnh tự động chạy trên DB production.

## Khi triển khai thật

- Dùng DB/user riêng, TLS và quyền tối thiểu theo nhà cung cấp; xác nhận backup tự động của nhà cung cấp và thử restore vào môi trường khác.
- Chạy migration như bước độc lập đã review, có backup trước đó; không gắn `database update` vào startup nhiều instance.
- Ghi migration ID, thời điểm, operator và kết quả smoke test. Nếu migration có thao tác mất dữ liệu, chuẩn bị phương án khôi phục trước khi chạy.
- `scripts/test-backend-isolated.ps1` là test local: tự tạo DB tạm, áp dụng migrations, kiểm tra nghiệp vụ rồi xóa DB tạm. Không chạy script này trên host production.
