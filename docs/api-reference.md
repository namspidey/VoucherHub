# VoucherHub — API đã triển khai

> Cập nhật refactor 09/10/2026: route/body nghiệp vụ giữ nguyên. Object lỗi nghiệp vụ hiện có thể là `application/problem+json`, bổ sung `status`, `title`, `type`, `traceId` nhưng giữ `message`/`code`; các JSON lỗi dưới đây minh họa field nghiệp vụ, không liệt kê mọi field của envelope. Xem [Architecture guide](architecture-guide.md).

Tài liệu đối chiếu với controller, request model và service hiện tại ngày 08/10/2026. Đây là hành vi của code đang có, chưa bao gồm các API dự kiến làm sau.

## 1. Cách dùng mẫu và chạy local

```powershell
# Tại D:\VoucherHub; Docker Desktop phải đang chạy
docker compose up -d
dotnet run --project backend/src/VoucherHub.Api --launch-profile http
```

- Base URL theo profile `http`: `http://localhost:5242`.
- Biến Postman `local_api`: `http://localhost:5242/api` (đã có `/api`, không thêm lần thứ hai).
- Body JSON: chọn **Body → raw → JSON**, header `Content-Type: application/json`.
- API cần JWT: **Authorization → Bearer Token**, chỉ dán token, không dán thêm chữ `Bearer` trong ô token.
- Mailpit local: `http://localhost:8025`. Email gửi vào đây, chưa gửi ra Gmail/Outlook thật.
- Khi đổi code, dừng backend cũ và chạy lại. Nếu build bỏ qua binary cũ, dùng `dotnet build backend/src/VoucherHub.Api --no-incremental` sau khi dừng backend.

### Nguồn của giá trị trong ví dụ

| Giá trị | Lấy từ đâu? | Có cố định không? |
|---|---|---|
| `local_api` | Profile chạy local; thay bằng URL backend khi deploy | Cấu hình môi trường, không phải dữ liệu nghiệp vụ |
| `email`, `password`, `displayName` | Người đăng ký nhập | Chuỗi ví dụ bên dưới không phải tài khoản seed sẵn |
| `verification_token` | Tham số `token` trong link email xác thực mới nhất | Phải lấy token thật; không dùng chuỗi placeholder |
| `access_token` | Response login của USER hoặc ADMIN | Phải lấy token thật; hết hạn thì login lại |
| `admin_token` | Response login của tài khoản có role ADMIN | Không lấy token USER để gọi API admin |
| `campaign_id` | `id` trả về khi tạo campaign | Phải dùng ID thật trong database |
| `offer_id` | `id` trả về khi tạo offer trong campaign đó | Phải dùng ID thật, đúng campaign |
| UUID trong response mẫu | Ví dụ để minh họa cấu trúc | Không copy làm ID thật nếu DB chưa có bản ghi đó |
| `name`, `description`, giá điểm, số lượng, quota | Admin nhập theo nghiệp vụ | Tự chọn giá trị hợp lệ |
| Lịch campaign, hạn offer | Admin nhập | Các ngày trong mẫu là ví dụ; thay lịch khi test |
| `status`, `createdBy`, timestamp hệ thống, `issuedQuantity` | Backend quyết định | Client không được điều khiển bằng body |

Thời gian request nên ghi ISO 8601 với offset rõ ràng, ví dụ `2026-10-10T18:00:00+07:00` hoặc `2026-10-10T11:00:00Z`. Backend chuyển sang UTC khi lưu. Response có `Z` là giờ UTC, chậm hơn giờ Việt Nam 7 tiếng. Số điểm dùng `long/bigint`; frontend JavaScript cần lưu ý các số rất lớn vượt `Number.MAX_SAFE_INTEGER`.

Các body không có field tương ứng trong request model sẽ không điều khiển được field hệ thống; JSON field lạ hiện thường bị bỏ qua. GET, publish, cancel và redeem không cần body. Các endpoint list mới có query phân trang/filter được mô tả ở phần cuối.

## 2. Danh sách endpoint

| Method | Path (sau base URL gốc) | Quyền | Thành công |
|---|---|---|---|
| POST | `/api/auth/register` | Public | 202 |
| POST | `/api/auth/resend-verification` | Public | 202 |
| POST | `/api/auth/verify-email` | Public | 200 |
| POST | `/api/auth/login` | Public | 200 |
| GET | `/api/users/me` | JWT hợp lệ | 200 |
| POST | `/api/admin/campaigns` | ADMIN + ACTIVE trong DB | 201 |
| GET | `/api/admin/campaigns/{id}` | ADMIN + ACTIVE trong DB | 200 |
| PUT | `/api/admin/campaigns/{id}` | ADMIN + ACTIVE trong DB | 200 |
| POST | `/api/admin/campaigns/{id}/offers` | ADMIN + ACTIVE trong DB | 201 |
| GET | `/api/admin/campaigns/{id}/offers` | ADMIN + ACTIVE trong DB | 200 |
| PUT | `/api/admin/campaigns/{id}/offers/{offerId}` | ADMIN + ACTIVE trong DB | 200 |
| POST | `/api/admin/campaigns/{id}/publish` | ADMIN + ACTIVE trong DB | 200 |
| POST | `/api/admin/campaigns/{id}/cancel` | ADMIN + ACTIVE trong DB | 200 |
| GET | `/api/campaigns` | Public | 200 |
| GET | `/api/campaigns/{id}` | Public | 200 |
| GET | `/api/admin/campaigns` | ADMIN + ACTIVE trong DB | 200 |
| GET | `/api/admin/users` | ADMIN + ACTIVE trong DB | 200 |
| POST | `/api/admin/users/{userId}/points/grants` | ADMIN + ACTIVE trong DB | 201 |
| GET | `/api/users/me/point-transactions` | JWT | 200 |
| GET | `/api/admin/users/{userId}/point-transactions` | ADMIN + ACTIVE trong DB | 200 |
| POST | `/api/voucher-offers/{offerId}/redeem` | USER + ACTIVE trong DB | 201 / 200 replay |
| GET | `/api/users/me/vouchers` | JWT | 200 |
| GET | `/api/users/me/vouchers/{id}` | JWT | 200 |
| POST | `/api/admin/users/{userId}/block` | ADMIN + ACTIVE trong DB | 200 |
| GET | `/api/admin/user-vouchers` | ADMIN + ACTIVE trong DB | 200 |
| GET | `/api/admin/audit-logs` | ADMIN + ACTIVE trong DB | 200 |
| GET | `/health/live` | Public | 200 |
| GET | `/health/ready` | Public | 200 / 503 |
| GET | `/openapi/v1.json` | Public, chỉ Development | 200 |

## 3. Response lỗi chung

### Validation — 400

Sai kiểu JSON, thiếu body/field bắt buộc, vượt độ dài hoặc vi phạm DataAnnotations có thể trả `ValidationProblemDetails`, khác với lỗi nghiệp vụ `{ "message": "..." }`.

Ví dụ register thiếu email (minh họa cấu trúc, nội dung framework/traceId có thể khác):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["The Email field is required."]
  },
  "traceId": "<trace-id-cua-request>"
}
```

Validation model xảy ra trước khi gọi service. Vì vậy có thể gặp lỗi validation trước lỗi trạng thái/missing record nếu body sai.

### JWT/phân quyền — 401, 403

| Trường hợp | HTTP | Response cần dựa vào |
|---|---|---|
| API bảo vệ nhưng thiếu JWT, JWT sai chữ ký hoặc hết hạn | 401 | Thường body rỗng; có thể có header `WWW-Authenticate: Bearer` |
| Token hợp lệ nhưng thiếu/sai user ID claim | 401 | Không có business message riêng |
| Token USER gọi admin API | 403 | Thường body rỗng |
| Token ADMIN nhưng user trong DB bị BLOCKED, hạ role hoặc không còn tồn tại | 403 | Thường body rỗng |

Login có JSON lỗi riêng (xem mục login). Các `NotFound()` không truyền object có thể được ASP.NET Core chuyển thành `ProblemDetails` status 404; không giả định luôn có field `message`. Path có `{id:guid}`/`{offerId:guid}` mà truyền chuỗi không phải GUID sẽ không match route (404).

### Rate limit — 429

- Register + resend + verify dùng chung policy `registration`: 10 request/phút/IP.
- Login dùng policy riêng: 5 request/phút/IP.
- Redeem: 20 request/phút/user ID từ JWT.
- Các hạn mức cấu hình qua `RateLimits`; không xếp hàng request. Response 429 hiện không có JSON message tùy chỉnh và không cấu hình `Retry-After` riêng.
- Chưa gắn các policy này vào `/me` hay campaign API.

### Lỗi hạ tầng — 500

PostgreSQL/SMTP không truy cập được có thể gây 500. API dùng exception handler và ProblemDetails cho lỗi bất ngờ; không dựa vào response này để phân loại lỗi nghiệp vụ. Ví dụ DB không chạy: server có thể log `Failed to connect to 127.0.0.1:5432`. Đây không phải lỗi input.

## 4. Register — POST `{{local_api}}/auth/register`

Không cần JWT.

```json
{
  "email": "user@example.com",
  "password": "VoucherHub@123",
  "displayName": "Nguyen Van A"
}
```

| Input | Bắt buộc / giới hạn | Nguồn và xử lý |
|---|---|---|
| `email` | Email hợp lệ, tối đa 255 ký tự | User nhập; service trim + lowercase trước truy vấn/lưu |
| `password` | 8–128 ký tự | User nhập; lưu hash, không lưu plaintext |
| `displayName` | Không trống, tối đa 100 ký tự | User nhập; trim trước lưu |

Model validation email chạy trước normalize trong service; nên gửi email không có khoảng trắng thừa.

Response **202 Accepted**:

```json
{
  "message": "If the email is eligible, verification instructions will be sent."
}
```

Các trường hợp cùng trả 202:

- Email mới: lưu pending và gửi email sau commit.
- Email đã có trong `users`: không tạo pending mới, không gửi email.
- Pending còn cooldown gửi lại: không sửa pending, không gửi email mới.
- Pending đã qua cooldown: cập nhật tên/password hash/token rồi gửi email mới.
- Request đăng ký đồng thời cùng email: tuần tự hóa theo email, không tạo hai pending.

**202 không bảo đảm đã có email mới.** Kiểm tra Mailpit và cooldown. Nếu SMTP lỗi sau commit, pending có thể đã lưu nhưng HTTP trả 500; có thể gửi lại sau cooldown.

Lỗi: 400 validation; displayName toàn khoảng trắng có lỗi `Display name must not be blank.` trong `errors.DisplayName`; 429 rate limit; 500 lỗi hạ tầng.

Chưa tạo user/ví ở bước register. Khi verify thành công, server cố định role ban đầu `USER`, status `ACTIVE`, balance `0`, sinh Guid và timestamp UTC. Không gửi các field này trong request.

## 5. Resend — POST `{{local_api}}/auth/resend-verification`

Không cần JWT.

```json
{
  "email": "user@example.com"
}
```

Email bắt buộc, đúng định dạng, tối đa 255 ký tự; dùng email thật đã register.

Response **202**:

```json
{
  "message": "If the email is eligible, verification instructions will be sent."
}
```

- Có pending và hết cooldown: tạo token mới, thay hash cũ, gia hạn token và gửi email.
- Không có pending (chưa đăng ký hoặc đã verify): vẫn 202, không gửi.
- Trong cooldown: vẫn 202, không gửi.
- Token cũ mất hiệu lực khi token mới được lưu thành công.
- Lỗi: 400 validation, 429, hoặc 500 nếu DB/SMTP lỗi.

## 6. Verify — POST `{{local_api}}/auth/verify-email`

Không cần JWT. Lấy token gốc từ link email mới nhất, không lấy token hash từ DB, không dùng access token login.

```json
{
  "token": "{{verification_token}}"
}
```

`token` bắt buộc, không trống; request model hiện chưa đặt max length. Backend SHA-256 token và tìm pending tương ứng.

**200 OK** — token đúng, còn hạn:

```json
{
  "message": "Email verification succeeded."
}
```

Trong cùng transaction: tạo user `USER/ACTIVE`, ví balance 0, xóa pending. Không trả access token; cần login tiếp.

**400 Bad Request** — sai/hết hạn/đã sử dụng/token cũ bị thay:

```json
{
  "message": "The verification token is invalid or expired."
}
```

**409 Conflict** — token pending còn tồn tại nhưng email đã có user chính thức, hoặc gặp unique email conflict:

```json
{
  "message": "The email is already registered."
}
```

Verify lại token đã thành công thường là **400**, vì pending đã bị xóa, không phải luôn 409. Thiếu token có thể trả 400 validation; thêm lỗi chung 429/500.

## 7. Login — POST `{{local_api}}/auth/login`

Không cần JWT. Dùng tài khoản đã verify.

```json
{
  "email": "user@example.com",
  "password": "VoucherHub@123"
}
```

| Input | Rule | Nguồn |
|---|---|---|
| `email` | Required, EmailAddress, tối đa 255 | Email đã đăng ký; service trim/lowercase |
| `password` | Required, tối đa 128; login không đặt min length 8 | Password thật của tài khoản; so với hash |

**200 OK**:

```json
{
  "accessToken": "<JWT-that-tra-ve-tu-login>",
  "tokenType": "Bearer",
  "expiresAtUtc": "2026-10-08T08:15:00Z"
}
```

- `accessToken`: server ký bằng secret; chứa ID/email/tên/role đọc từ DB và `jti` ngẫu nhiên.
- `tokenType`: cố định `Bearer`.
- `expiresAtUtc`: giờ server hiện tại + TTL cấu hình, mẫu trên không phải hạn cố định.
- Hiện chưa có refresh token/logout API. Token hết hạn thì login lại.

**401 Unauthorized** — email không tồn tại, chưa verify hoặc password sai:

```json
{
  "message": "Invalid email or password."
}
```

**403 Forbidden** — password đúng nhưng tài khoản BLOCKED:

```json
{
  "message": "The account is blocked."
}
```

Password sai của user BLOCKED vẫn nhận 401. Thêm lỗi chung 400 validation, 429, 500.

## 8. Profile — GET `{{local_api}}/users/me`

Header: `Authorization: Bearer {{access_token}}`. Không body, không gửi user ID.

**200 OK**:

```json
{
  "id": "11111111-1111-4111-8111-111111111111",
  "email": "user@example.com",
  "displayName": "Nguyen Van A",
  "role": "USER",
  "status": "ACTIVE",
  "emailVerifiedAt": "2026-10-08T07:50:00Z",
  "balance": 0
}
```

User ID dùng để truy vấn lấy từ JWT; toàn bộ field response đọc từ `users` join `point_wallets`. Balance là số dư thật, không cố định 0; hiện user mới có 0 vì chưa có API cấp điểm.

- 401: thiếu/sai/hết hạn token hoặc user ID claim không phải GUID.
- 404: không tìm thấy cặp user + wallet (kể cả user có nhưng thiếu ví); không có business message riêng.
- 500: lỗi DB.
- **Hành vi hiện tại:** user BLOCKED còn JWT hợp lệ vẫn có thể đọc `/me`; endpoint này không kiểm tra status để từ chối. ADMIN cũng đọc được profile chính mình. Role/status trong response là DB hiện tại, có thể khác role cũ chứa trong token.

## 9. Admin: quyền và dữ liệu chung

Mọi endpoint campaign dưới đây cần `Authorization: Bearer {{admin_token}}`. Backend kiểm tra cả claim ADMIN lẫn user hiện tại trong DB có role ADMIN và status ACTIVE. `createdBy`/actor audit lấy từ JWT, không nhận từ body.

Local chưa có API public tạo admin. Có thể cấp role cho một tài khoản riêng đã verify bằng script, thay email thật:

```powershell
Get-Content -Raw ./scripts/bootstrap-admin.sql |
  docker compose exec -T postgres psql -U voucherhub -d voucherhub -v "admin_email=admin@example.com"
```

Script không tạo/verify tài khoản; kiểm tra dòng kết quả có ADMIN. Login lại sau khi đổi role để token có claim mới. Không coi email ví dụ là tài khoản có sẵn.

### Body campaign dùng cho POST và PUT

```json
{
  "name": "Flash Sale tháng 10",
  "description": "Campaign đầu tiên",
  "startsAt": "2026-10-10T18:00:00+07:00",
  "endsAt": "2026-10-10T20:00:00+07:00",
  "maxRedemptionsPerUser": 3
}
```

| Field | Rule | Nguồn/default |
|---|---|---|
| `name` | Required, không trắng, tối đa 200 | Admin nhập; trim |
| `description` | Optional/null, tối đa 2000 | Admin nhập; trim, trắng thành null |
| `startsAt` | Bắt buộc có giá trị thời gian hợp lệ | Admin chọn; chuyển UTC |
| `endsAt` | Bắt buộc, sau startsAt | Admin chọn; chuyển UTC |
| `maxRedemptionsPerUser` | Số nguyên 1–2147483647 | Admin nhập; bỏ field mặc định **1**, gửi 0/null là lỗi |

Quota thuộc toàn campaign, không phải mỗi offer. Tạo/sửa Draft cho phép lưu lịch đã qua nếu khoảng thời gian hợp lệ; publish mới kiểm tra chưa kết thúc.

### Response campaign chung

POST tạo trả 201; GET/PUT/publish/cancel thành công trả 200 với cùng cấu trúc:

```json
{
  "id": "22222222-2222-4222-8222-222222222222",
  "name": "Flash Sale tháng 10",
  "description": "Campaign đầu tiên",
  "startsAt": "2026-10-10T11:00:00Z",
  "endsAt": "2026-10-10T13:00:00Z",
  "status": "DRAFT",
  "maxRedemptionsPerUser": 3,
  "createdBy": "11111111-1111-4111-8111-111111111111",
  "createdAt": "2026-10-08T08:00:00Z",
  "updatedAt": "2026-10-08T08:00:00Z"
}
```

- `id`: server sinh khi tạo; dùng ID thật trả về cho các request sau.
- Tên/mô tả/lịch/quota: input đã chuẩn hóa hoặc dữ liệu DB khi GET.
- `status`: server quản lý DRAFT → PUBLISHED/CANCELLED; không gửi để tự đổi trạng thái.
- `createdBy`: user ID từ JWT; `createdAt`/`updatedAt`: giờ UTC server. Sau sửa/chuyển trạng thái chỉ updatedAt đổi, createdAt giữ nguyên.

## 10. Tạo campaign — POST `{{local_api}}/admin/campaigns`

Body: dùng mẫu campaign mục 9. Không gửi ID/status/createdBy.

- **201 Created**: response campaign chung, status cố định DRAFT. Header `Location` trỏ GET campaign vừa tạo.
- **400** validation model hoặc lỗi nghiệp vụ:

```json
{
  "message": "Name, time range or campaign redemption quota is invalid."
}
```

- 401/403/500: như mục lỗi chung.
- Ghi `CAMPAIGN_CREATED` cùng transaction. Gọi POST lại tạo campaign khác; hiện không có idempotency key cho tạo campaign.

## 11. Đọc campaign — GET `{{local_api}}/admin/campaigns/{{campaign_id}}`

`campaign_id`: Guid thật từ POST, đặt ở path; không body.

- **200**: response campaign chung theo DB, đọc được cả Draft/Published/Cancelled.
- **404**: GUID không tồn tại (không có business message riêng), hoặc path không phải GUID.
- 401/403/500: như mục lỗi chung.
- Chỉ trả campaign, chưa kèm offers; gọi API list offers riêng.

## 12. Sửa campaign — PUT `{{local_api}}/admin/campaigns/{{campaign_id}}`

Path ID thật, body đầy đủ theo mục 9. Đây là PUT thay các field cấu hình, không phải PATCH: bỏ description sẽ đưa về null, bỏ quota sẽ về default 1. Không tự giữ field bỏ sót.

- **200**: response campaign chung với cấu hình mới; status vẫn DRAFT, ghi `CAMPAIGN_UPDATED` có before/after.
- **400** lỗi model, hoặc một trong các JSON sau:

```json
{ "message": "Invalid campaign configuration." }
```

```json
{ "message": "All offer expiry times must remain after the campaign end time." }
```

- **404** campaign không tồn tại:

```json
{ "message": "Campaign was not found." }
```

- **409** campaign không còn Draft:

```json
{ "message": "Only Draft campaigns can be edited." }
```

- 401/403/500: như mục lỗi chung. Đổi endsAt bị chặn nếu bằng/vượt expiresAt của bất kỳ offer hiện có.

## 13. Offer: body và response chung

Body dùng khi tạo/sửa offer:

```json
{
  "name": "Voucher giảm 50K",
  "description": "Đổi bằng 100 điểm",
  "pointsCost": 100,
  "totalQuantity": 20,
  "expiresAt": "2026-10-31T23:59:00+07:00"
}
```

| Field | Rule | Nguồn/default |
|---|---|---|
| `name` | Required, không trắng, tối đa 200 | Admin nhập; trim |
| `description` | Optional/null, tối đa 2000 | Admin nhập; trim, trắng thành null |
| `pointsCost` | Số nguyên > 0, C# long | Admin nhập; không phải giá tiền VND |
| `totalQuantity` | Số nguyên 1–2147483647 | Admin nhập |
| `expiresAt` | Có giá trị, sau campaign.endsAt | Admin chọn; chuyển UTC |

Không có default hợp lệ cho pointsCost/totalQuantity: bỏ field thành 0 và bị validation từ chối. Không gửi campaignId/issuedQuantity/status trong body.

Response offer chung (POST 201 hoặc PUT 200):

```json
{
  "id": "33333333-3333-4333-8333-333333333333",
  "campaignId": "22222222-2222-4222-8222-222222222222",
  "name": "Voucher giảm 50K",
  "description": "Đổi bằng 100 điểm",
  "pointsCost": 100,
  "totalQuantity": 20,
  "issuedQuantity": 0,
  "expiresAt": "2026-10-31T16:59:00Z",
  "createdAt": "2026-10-08T08:05:00Z",
  "updatedAt": "2026-10-08T08:05:00Z"
}
```

ID/timestamp do backend sinh, campaignId lấy từ path đã kiểm tra DB; issuedQuantity ban đầu server đặt 0, khi GET là số DB thực tế. Giá/tổng lượng/hạn voucher lấy input đã lưu. Offer mutation cũng cập nhật campaign.updatedAt.

## 14. Thêm offer — POST `{{local_api}}/admin/campaigns/{{campaign_id}}/offers`

Body theo mục 13; path dùng campaign ID thật.

- **201** response offer chung; `Location` trỏ API list offers của campaign.
- **400** validation hoặc:

```json
{ "message": "Offer requires a name, positive price/quantity and expiry after campaign end." }
```

- **404**:

```json
{ "message": "Campaign was not found." }
```

- **409**:

```json
{ "message": "Offers can only be changed in Draft campaigns." }
```

- 401/403/500: như mục lỗi chung. Ghi `VOUCHER_OFFER_CREATED` cùng transaction. Gọi lại POST tạo offer khác; không tự deduplicate theo tên.

## 15. List offers — GET `{{local_api}}/admin/campaigns/{{campaign_id}}/offers`

Không body. Campaign ID lấy từ dữ liệu thật.

**200** trả mảng, mỗi phần tử theo response offer mục 13:

```json
[
  {
    "id": "33333333-3333-4333-8333-333333333333",
    "campaignId": "22222222-2222-4222-8222-222222222222",
    "name": "Voucher giảm 50K",
    "description": "Đổi bằng 100 điểm",
    "pointsCost": 100,
    "totalQuantity": 20,
    "issuedQuantity": 0,
    "expiresAt": "2026-10-31T16:59:00Z",
    "createdAt": "2026-10-08T08:05:00Z",
    "updatedAt": "2026-10-08T08:05:00Z"
  }
]
```

- Campaign tồn tại, chưa có offer: **200** body `[]`.
- Campaign không tồn tại: **404**, không có business message riêng.
- 401/403/500: như mục lỗi chung.
- Sắp xếp CreatedAt rồi Id, chưa phân trang; có thể đọc offers của Draft/Published/Cancelled bằng quyền admin.

## 16. Sửa offer — PUT `{{local_api}}/admin/campaigns/{{campaign_id}}/offers/{{offer_id}}`

Hai ID phải có thật, offer phải thuộc đúng campaign. Body đầy đủ mục 13; bỏ description sẽ đưa về null.

- **200** response offer chung với field mới; ghi `VOUCHER_OFFER_UPDATED` before/after.
- **400** lỗi model hoặc message về tên/giá/lượng/hạn như API tạo offer.
- **404** campaign không tồn tại: `{ "message": "Campaign was not found." }`.
- **404** offer không tồn tại hoặc thuộc campaign khác:

```json
{ "message": "Offer was not found in this campaign." }
```

- **409** campaign không còn Draft: `{ "message": "Offers can only be changed in Draft campaigns." }`.
- **409** offer trong Draft nhưng đã có issuedQuantity khác 0 (phòng dữ liệu bất thường):

```json
{ "message": "An offer with issued vouchers cannot be edited." }
```

- 401/403/500: như mục lỗi chung.

## 17. Publish — POST `{{local_api}}/admin/campaigns/{{campaign_id}}/publish`

Không body. Client không gửi `{ "status": "PUBLISHED" }`; backend thực hiện chuyển trạng thái.

- **200** response campaign mục 9, `status` bằng `PUBLISHED`, updatedAt mới. Ghi `CAMPAIGN_PUBLISHED`.
- **400** campaign đã kết thúc hoặc cấu hình không hợp lệ:

```json
{ "message": "Publish requires a valid campaign that has not ended." }
```

- **400** chưa có offer hoặc offer không hợp lệ (giá/lượng/hạn/tên/issuedQuantity):

```json
{ "message": "Publish requires at least one valid offer with expiry after campaign end." }
```

- **404**: `{ "message": "Campaign was not found." }`.
- **409** đã Published hoặc Cancelled:

```json
{ "message": "Only Draft campaigns can be published." }
```

- 401/403/500: như mục lỗi chung.

Campaign đang diễn ra có thể publish nếu chưa kết thúc. Publish xong khóa cấu hình campaign và offers. Hai request publish đồng thời được tuần tự hóa: một 200, một 409, chỉ một audit. Publish lại **không** trả thành công như cancel lặp lại.

## 18. Cancel — POST `{{local_api}}/admin/campaigns/{{campaign_id}}/cancel`

Không body; dùng ID thật.

- **200** khi Draft/Published: response campaign mục 9, status `CANCELLED`, updatedAt mới; ghi `CAMPAIGN_CANCELLED`.
- **200** khi đã Cancelled: response trạng thái hiện tại, không thêm audit và không đổi updatedAt.
- **404**: `{ "message": "Campaign was not found." }`.
- **409** nếu trạng thái ngoài DRAFT/PUBLISHED/CANCELLED (không xảy ra với schema hợp lệ hiện tại):

```json
{ "message": "Campaign cannot be cancelled from its current status." }
```

- 401/403/500: như mục lỗi chung.

Không thu hồi voucher, không hoàn điểm. Không có chuyển Cancelled về Draft/Published. Thao tác không xóa campaign hay offers.

## 19. OpenAPI và health

### GET `http://localhost:5242/openapi/v1.json`

Chỉ được map khi Environment là Development, không cần token/body. 200 trả tài liệu OpenAPI JSON được sinh từ endpoint metadata. Các field chính gồm `openapi`, `info`, `paths`, `components`; nội dung/version thay đổi theo framework và các API. Ngoài Development route này không được map (404). Đây không phải giao diện Swagger UI.

`GET /health/live` trả `{ "status": "live" }`. `GET /health/ready` thử kết nối PostgreSQL, trả `{ "status": "ready" }` hoặc HTTP 503. Cả hai không cần JWT.

## 20. Cấu hình, giá trị cố định và giới hạn hiện tại

| Nội dung | Hiện tại | Lấy từ đâu? |
|---|---|---|
| Token email TTL | 15 phút | `Registration:VerificationTokenLifetimeMinutes`, override được |
| Pending retention | 24 giờ | `Registration:PendingRegistrationRetentionHours`, override được |
| Cooldown gửi lại | 60 giây | `Registration:ResendCooldownSeconds`, override được |
| Link xác thực local | `http://localhost:5173/verify-email` | `Registration:VerificationPageUrl` |
| JWT TTL | 15 phút | `Jwt:AccessTokenLifetimeMinutes` |
| JWT issuer/audience | `VoucherHub` / `VoucherHub.Client` | Cấu hình `Jwt` |
| JWT signing key | Secret thật | User Secrets/environment; không copy vào docs/source |
| JWT clock skew | 30 giây | Cố định trong Program.cs; validator có khoảng dung sai thời gian |
| Email token | Random 32 byte, hash SHA-256 | Sinh trong service; không fixed token |
| Role/status user mới | USER / ACTIVE | Cố định trong luồng verify hiện tại |
| Wallet balance ban đầu | 0 | Cố định trong luồng verify + default schema |
| Campaign mới | DRAFT | Cố định phía server |
| Quota nếu bỏ field | 1 | Default request model/entity/schema; admin có thể gửi số dương khác |
| Offer issuedQuantity mới | 0 | Cố định phía server/schema |
| Rate limit | 10 registration, 5 login/phút/IP; 20 redeem/phút/user | Config `RateLimits`, bộ nhớ của instance |
| SMTP local | localhost:1025, SSL false | `Smtp` config, Mailpit; thay provider thật khi deploy |
| Public campaign cache | 2 giây | HybridCache một instance, tag `campaigns`; response có tồn kho |
| Frontend CORS local | `http://localhost:5173` | `Cors:AllowedOrigins` |

Pending cleanup chạy khi register và bằng background service mỗi giờ; xóa dòng có `created_at` quá retention. SMTP gửi sau commit, chưa có queue/retry/outbox tự động. Cache và limiter dùng bộ nhớ một instance.

Các API **chưa triển khai**: refresh token, logout, cấp role ADMIN qua HTTP, unblock user, sửa profile, sử dụng/hoàn voucher và mua điểm. Không có API nhận role ADMIN từ người đăng ký.

## 21. Thứ tự test Postman

1. Register bằng email tự chọn, mở Mailpit lấy token.
2. Verify token mới nhất, login lấy access token, gọi `/users/me`.
3. Với admin: register/verify tài khoản riêng, chạy script bootstrap local, login lại lấy admin token.
4. Tạo campaign, lưu `id` vào `campaign_id`.
5. Thêm offer, lưu `id` vào `offer_id`; GET list để đối chiếu.
6. Khi còn Draft: thử PUT campaign/offer; thử giá 0, quota 0, hạn offer bằng endsAt để thấy 400.
7. Publish campaign có lịch chưa kết thúc. Sau publish, thử PUT để thấy 409.
8. Cancel; gọi cancel lần nữa vẫn 200. Publish lại campaign Cancelled sẽ 409.
9. Thử token USER cho admin route để thấy 403; bỏ token để thấy 401.

Có thể dùng Postman Scripts sau login để lưu token (chỉ lưu khi 200):

```javascript
if (pm.response.code === 200) {
  pm.environment.set("access_token", pm.response.json().accessToken);
}
```

Khi login admin, đổi tên biến lưu thành `admin_token`. Sau POST campaign/offer thành công (201), có thể lưu ID tương tự:

```javascript
if (pm.response.code === 201) {
  pm.environment.set("campaign_id", pm.response.json().id);
}
```

Script trên chỉ dùng cho POST tạo campaign; với POST tạo offer thay tên biến thành `offer_id`. Không ghi password/token thật vào tài liệu hoặc collection chia sẻ.

## 22. Public campaign và các danh sách mới

`GET {{local_api}}/campaigns?page=1&pageSize=20&phase=ONGOING` là public. `phase` tùy chọn: `UPCOMING`, `ONGOING`, `ENDED`; tính theo giờ UTC hiện tại, không lưu DB. Chỉ campaign PUBLISHED xuất hiện. `GET {{local_api}}/campaigns/{{campaign_id}}` trả 404 nếu Draft/Cancelled/không tồn tại. Response detail:

```json
{
  "id": "22222222-2222-4222-8222-222222222222",
  "name": "Flash Sale tháng 10",
  "description": "Campaign đầu tiên",
  "startsAt": "2026-10-10T11:00:00Z",
  "endsAt": "2026-10-10T13:00:00Z",
  "status": "PUBLISHED",
  "phase": "UPCOMING",
  "maxRedemptionsPerUser": 3,
  "offers": [
    {
      "id": "33333333-3333-4333-8333-333333333333",
      "name": "Voucher giảm 50K",
      "description": "Đổi bằng 100 điểm",
      "pointsCost": 100,
      "totalQuantity": 20,
      "issuedQuantity": 0,
      "remainingQuantity": 20,
      "expiresAt": "2026-10-31T16:59:00Z"
    }
  ]
}
```

Giá, số lượng và thời gian là DB thật; `remainingQuantity` chỉ để hiển thị, có thể trễ tối đa khoảng TTL cache 2 giây. Khi đổi voucher, backend đọc lại DB. List trả `{ "items": [<campaign như trên>], "pageNumber": 1, "pageSize": 20, "totalCount": 1 }`.

`GET {{local_api}}/admin/campaigns?page=1&pageSize=20&status=DRAFT&search=Flash` cần admin ACTIVE. Search theo tên không phân biệt hoa/thường; status tùy chọn DRAFT/PUBLISHED/CANCELLED. Items dùng response campaign mục 9, không kèm offers. `GET {{local_api}}/admin/users?page=1&pageSize=20&status=ACTIVE&email=user` trả id, email, displayName, role, status, balance, createdAt, updatedAt. Balance từ ví DB, không cố định.

Mọi list mới: page mặc định 1, pageSize mặc định 20 và tối đa 100; filter sai trả 400 `{ "code": "INVALID_FILTER" }` hoặc `{ "code": "INVALID_PAGING_OR_TYPE" }` tùy endpoint. Các list admin trả 403 nếu token admin không còn ACTIVE trong DB.

## 23. Admin cấp điểm

`POST {{local_api}}/admin/users/{{user_id}}/points/grants`; user_id lấy từ admin list hoặc `/users/me` của user đích. JWT phải là admin ACTIVE. Body:

```json
{
  "amount": 500,
  "reason": "Điểm tham gia sự kiện"
}
```

Amount là điểm dương kiểu long; reason do admin nhập, trim, không trắng và tối đa 500 ký tự. Chỉ cấp cho tài khoản USER ACTIVE có ví. Response **201**:

```json
{
  "transactionId": "44444444-4444-4444-8444-444444444444",
  "userId": "11111111-1111-4111-8111-111111111111",
  "amount": 500,
  "balanceAfter": 500,
  "performedBy": "55555555-5555-4555-8555-555555555555",
  "reason": "Điểm tham gia sự kiện",
  "createdAt": "2026-10-08T08:00:00Z"
}
```

Transaction ID, performedBy và timestamp do server tạo/lấy từ JWT; balanceAfter từ ví đã khóa. Đồng thời ghi `point_transactions` và audit trong cùng DB transaction. User không tồn tại: 404 `USER_NOT_FOUND`; user không ACTIVE: 409 `USER_NOT_ACTIVE`; ví thiếu: 409 `WALLET_MISSING`; tràn long: 409 `BALANCE_OVERFLOW`; input sai: 400 validation hoặc `INVALID_GRANT`.

Endpoint này **chưa có idempotency key**. Nếu client timeout sau khi gửi, kiểm tra lịch sử trước khi gửi lại, vì POST lần nữa sẽ cộng thêm điểm.

`GET {{local_api}}/users/me/point-transactions?page=1&pageSize=20&type=ADMIN_GRANT` đọc ví của JWT; type tùy chọn `ADMIN_GRANT` hoặc `VOUCHER_REDEMPTION`. Admin có `GET {{local_api}}/admin/users/{{user_id}}/point-transactions` với query tương tự. Items gồm id, type, amount có dấu, balanceAfter, userVoucherId (null khi grant), performedBy, reason, createdAt. List phân trang như mục 22; user/ví thiếu trả 404.

## 24. Đổi voucher và xem voucher đã nhận

`POST {{local_api}}/voucher-offers/{{offer_id}}/redeem` dùng JWT của USER ACTIVE. Offer ID lấy từ public campaign detail hoặc response tạo offer. **Không gửi body**. Trong Headers đặt:

```http
Authorization: Bearer <access_token_thật>
Idempotency-Key: 12345678-1234-4234-8234-123456789abc
```

Client tạo UUID mới cho mỗi lần chủ động đổi; khi retry đúng thao tác thì giữ nguyên UUID. Một user có thể dùng key khác để đổi cùng offer thêm lần nữa trong quota campaign. Server lấy campaign, giá, tồn kho, hạn và user ID từ DB/JWT; client không được gửi các giá trị đó để quyết định lượt đổi.

**201 Created** lần đầu; **200 OK** khi retry cùng key và offer, cùng voucher ID/code và không trừ điểm lần nữa:

```json
{
  "id": "66666666-6666-4666-8666-666666666666",
  "voucherOfferId": "33333333-3333-4333-8333-333333333333",
  "campaignId": "22222222-2222-4222-8222-222222222222",
  "offerName": "Voucher giảm 50K",
  "campaignName": "Flash Sale tháng 10",
  "code": "A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6",
  "pointsSpent": 100,
  "claimedAt": "2026-10-10T11:05:00Z",
  "expiresAt": "2026-10-31T16:59:00Z",
  "isExpired": false
}
```

Code mẫu chỉ minh họa; server sinh ngẫu nhiên 24 byte, encode hex 48 ký tự, unique. Không copy code mẫu để dùng. `pointsSpent`/`expiresAt` chụp khi phát; `isExpired` tính theo thời điểm đọc. Quota đếm toàn campaign, kể cả voucher đã hết hạn.

| Tình huống | HTTP | `code` |
|---|---|---|
| Thiếu/sai `Idempotency-Key` | 400 | `INVALID_REQUEST_KEY` |
| Key đã dùng cho offer khác | 409 | `IDEMPOTENCY_KEY_REUSED` |
| User không ACTIVE/không phải USER | 403 | Body có thể rỗng |
| Offer không có | 404 | `OFFER_NOT_FOUND` |
| Campaign chưa publish/cancel | 409 | `CAMPAIGN_NOT_PUBLISHED` |
| Chưa đến giờ | 409 | `CAMPAIGN_NOT_STARTED` |
| Đã hết giờ | 409 | `CAMPAIGN_ENDED` |
| Hết quota toàn campaign | 409 | `CAMPAIGN_QUOTA_EXCEEDED` |
| Không đủ điểm | 409 | `INSUFFICIENT_POINTS` |
| Hết stock | 409 | `OUT_OF_STOCK` |

Mẫu 409: `{ "code": "INSUFFICIENT_POINTS" }`. Thứ tự kiểm tra là user/key → campaign/time → quota → balance → stock; nếu nhiều điều kiện cùng sai, client nhận lỗi được kiểm tra trước. Không có record thành công cho request thất bại.

`GET {{local_api}}/users/me/vouchers?page=1&pageSize=20&expired=false` và `GET {{local_api}}/users/me/vouchers/{{voucher_id}}` chỉ đọc voucher của JWT. List có `{ items, pageNumber, pageSize, totalCount }`; mỗi item theo response trên. `expired` tùy chọn true/false. ID voucher thuộc user khác trả 404; mã chỉ gửi cho chủ sở hữu. User BLOCKED còn JWT hợp lệ vẫn đọc được lịch sử của mình.

## 25. Block và giám sát admin

`POST {{local_api}}/admin/users/{{user_id}}/block`:

```json
{ "reason": "Lạm dụng hệ thống" }
```

Reason bắt buộc, trim, không trắng, tối đa 500. Chỉ block USER; target ADMIN trả 409 `{ "code": "CANNOT_BLOCK_ADMIN" }`. User thiếu trả 404 `USER_NOT_FOUND`. Thành công 200:

```json
{
  "id": "11111111-1111-4111-8111-111111111111",
  "status": "BLOCKED",
  "updatedAt": "2026-10-08T09:00:00Z"
}
```

Block lặp lại trả 200 trạng thái cũ, không ghi audit trùng. Lần đầu ghi `USER_BLOCKED` kèm lý do trong audit. JWT cũ của user bị block không đổi được voucher; login mới bị chặn.

`GET {{local_api}}/admin/user-vouchers?page=1&pageSize=20` xem lịch sử phát. Filter tùy chọn `userId`, `campaignId`, `offerId`, `from`, `to`; to là mốc không bao gồm. Items có id, userId, voucherOfferId, campaignId, pointsSpent, claimedAt, expiresAt; **không trả code**.

`GET {{local_api}}/admin/audit-logs?page=1&pageSize=20` xem audit. Filter tùy chọn `actorUserId`, `action`, `entityType`, `entityId`, `from`, `to`. Items có id, actorUserId, action, entityType, entityId, details (chuỗi JSON), createdAt. Không có API sửa/xóa audit. Với cả hai endpoint, page/pageSize như mục 22; from phải trước to; filter sai trả 400 `INVALID_FILTER`.
