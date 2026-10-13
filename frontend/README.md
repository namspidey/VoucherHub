# VoucherHub Frontend

Frontend React + Vite cho VoucherHub. Giao diện dùng Bootstrap, Bootstrap Icons và Tailwind CSS; dữ liệu lấy từ ASP.NET Core API.

## Chạy local

Backend mặc định chạy tại `http://localhost:5242`, frontend tại `http://localhost:5173`.

```cmd
cd /d D:\VoucherHub\frontend
npm install
npm run dev
```

Nếu backend dùng URL khác, copy `.env.example` thành `.env.local` và sửa:

```env
VITE_API_BASE_URL=http://localhost:5242/api
```

Khởi động backend từ thư mục gốc:

```cmd
docker compose up -d
dotnet run --project backend/src/VoucherHub.Api --launch-profile http
```

## Kiểm tra

```cmd
npm run lint
npm run build
```

## Nhóm màn hình

- Public: danh sách/chi tiết campaign, đăng ký, xác thực email, đăng nhập.
- User: profile và số dư, đổi voucher, voucher đã nhận, lịch sử điểm.
- Admin: dashboard, campaign và offer, user/cấp điểm/khóa tài khoản, voucher đã phát, audit log.

JWT được lưu trong `localStorage` cho MVP. Khi token hết hạn hoặc API trả 401, frontend xóa session và yêu cầu đăng nhập lại.
