# VoucherHub — Kế hoạch hoàn thiện backend trước deploy

Ngày đối chiếu source: **08/10/2026**. Workspace: `D:\VoucherHub`.

Tài liệu này ghi kế hoạch triển khai ban đầu và các tiêu chí nghiệm thu. Sau khi hoàn thành, dùng [api-reference.md](api-reference.md) làm nguồn chuẩn cho URL, input và response đang chạy.

**Cập nhật sau triển khai:** Các chặng backend MVP bắt buộc bên dưới đã được làm trong source. Danh sách endpoint hiện chạy và response thực tế nằm trong [api-reference.md](api-reference.md); một số contract đề xuất ở tài liệu này được điều chỉnh nhỏ khi code (ví dụ list trả `pageNumber`, `pageSize`). Build Release và bộ test trên PostgreSQL tạm đã pass; xem [README](../README.md) để chạy lại. SignalR/Redis vẫn là tùy chọn. Việc chọn host, SMTP thật, DNS và secrets production nằm ở mục 13.

Mục tiêu: backend MVP chạy và kiểm thử đầy đủ tại local, có thể bàn giao cho frontend và chuẩn bị cấu hình production. Chưa mua host, tạo dịch vụ cloud, cấu hình DNS hoặc deploy thật trong công việc này.

## 1. Phạm vi giữ nguyên

- .NET 10, ASP.NET Core, EF Core, PostgreSQL 17; monolith bốn project Domain/Application/Infrastructure/Api.
- Hai role ADMIN/USER; trạng thái tài khoản ACTIVE/BLOCKED.
- Giữ đúng tám bảng nghiệp vụ: `users`, `pending_registrations`, `point_wallets`, `campaigns`, `voucher_offers`, `user_vouchers`, `point_transactions`, `audit_logs`. Bảng lịch sử migration của EF không phải bảng nghiệp vụ bổ sung.
- Guid/UUID cho ID; snake_case trong PostgreSQL; thời gian UTC; điểm dùng long/bigint.
- Quota đổi thuộc **toàn campaign**. User có thể đổi cùng offer nhiều lần trong quota bằng những request key khác nhau.
- `user_vouchers` đồng thời là lịch sử đổi thành công. Không thêm bảng `voucher_redemptions`.
- Không thêm merchant/category, kho mã nhập sẵn, thanh toán/mua điểm, chuyển điểm, sử dụng voucher tại cửa hàng, hoàn điểm, hủy voucher đã nhận hoặc xóa cứng user.
- Chưa thêm refresh token, quên mật khẩu, sửa profile, đăng nhập mạng xã hội hoặc quản trị role qua HTTP.
- Không mặc định thêm generic repository, CQRS/MediatR, microservices, broker, outbox hoặc hệ thống queue.
- Redis và SignalR không chặn việc nghiệm thu MVP. HybridCache cho dữ liệu đọc nằm trong kế hoạch chính, triển khai sau khi transaction đúng.

## 2. Đã có gì trong source

Đây là kết quả đọc file, không phải xác nhận tình trạng DB hoặc tiến trình đang chạy tại thời điểm người khác mở tài liệu.

| Hạng mục | Hiện trạng | Việc cần tiếp |
|---|---|---|
| Schema | Có tám entity và mapping; ba migration CreateUsers, CreatePointWallets, CreateRemainingMvpTables | Kiểm tra migration history thực tế trước thay đổi DB |
| Đăng ký | Pending, password hash, token hash, cooldown, verify tạo user + ví trong transaction | Test đầy đủ race/expiry, giới hạn input, cleanup định kỳ |
| Email | SMTP sender có cấu hình; local dùng Mailpit | Validate config production, xử lý lỗi gửi; kiểm tra gửi thật ở giai đoạn deploy |
| Login | JWT access token; chặn BLOCKED khi login | Giữ scope access token, kiểm tra quyền hiện tại tại nghiệp vụ nhạy cảm |
| Profile | GET `/api/users/me`, join user + ví | Test; ghi rõ BLOCKED vẫn có thể đọc profile bằng JWT còn hạn |
| Admin campaign | Create/get/update, create/list/update offer, publish/cancel | Bổ sung list campaign; giữ rule Draft và audit |
| Lock campaign | Mutation khóa actor FOR SHARE, campaign FOR UPDATE | Phối hợp thứ tự lock với redeem/block/grant |
| Điểm | Có ví, bảng lịch sử và constraint | Chưa có API cấp điểm hoặc lịch sử |
| Đổi voucher | Có bảng/constraint | Chưa có luồng đổi, sinh mã, idempotency hoặc API voucher của user |
| Audit | Campaign mutation đã ghi audit trong transaction | Bổ sung grant/block/redeem; API đọc audit |
| Cache/realtime | Chưa thấy triển khai | Cache sau core; SignalR tùy chọn |
| Kiểm thử | Có `scripts/test-create-campaign.ps1` và bootstrap admin local | Bổ sung test tự động toàn luồng và tranh chấp trên PostgreSQL |
| Vận hành | Docker Compose PostgreSQL/Mailpit; OpenAPI Development | Error handling, health, CORS, cấu hình, container build và hướng dẫn |

Các constraint quan trọng đã có trong mapping:

- Unique email user/pending; unique ví theo user; balance >= 0, mặc định 0; FK Restrict.
- Campaign status hợp lệ, ends_at > starts_at, quota > 0.
- Offer giá > 0, lượng > 0, 0 <= issued_quantity <= total_quantity.
- Unique mã voucher và `(user_id, request_key)`; index `(user_id, voucher_offer_id)` hiện **không unique**, phải giữ như vậy.
- Unique `point_transactions.user_voucher_id` để tối đa một giao dịch gắn với một voucher; ADMIN_GRANT dương và không có voucher, VOUCHER_REDEMPTION âm và có voucher; balance_after >= 0.

Constraint cuối chỉ bảo đảm **tối đa một** bản ghi trừ điểm cho một voucher. Service + transaction + test phải bảo đảm **có đúng một** và amount bằng âm points_spent, ví thuộc đúng người nhận. Các điều kiện liên bảng này chưa được bảo đảm chỉ bằng FK hiện có.

## 3. Thứ tự triển khai và điểm dừng kiểm tra

| Chặng | Kết quả cần đạt | Phụ thuộc |
|---|---|---|
| 0 | Đọc repo, xác nhận baseline build/test/DB và chốt contract nhỏ | Không |
| 1 | Admin cấp điểm; user đọc lịch sử điểm | Schema, auth hiện tại |
| 2 | Campaign list/detail cho user; list campaign/admin users | Campaign hiện tại |
| 3 | Đổi voucher + danh sách/chi tiết voucher của mình | 1, 2 và thiết kế lock |
| 4 | Khóa user; lịch sử phát voucher và audit cho admin | 3; rà lại race với block |
| 5 | Hoàn thiện lỗi, auth/cleanup, rate limit và cache | Core đã ổn định |
| 6 | Test tích hợp/concurrency, tài liệu và chuẩn bị chạy Production local | Tất cả chặng chính |
| Tùy chọn | SignalR sau commit | 3, 5 đã hoàn thành |

Mỗi chặng phải build, có test phù hợp và mẫu request để người học thử được. Có thể viết test song song với code trong từng chặng; không đợi chặng 6 mới phát hiện sai transaction. Không coi build thành công là hoàn thành nghiệp vụ.

## 4. Contract chung cho API mới

- Danh tính người gọi lấy từ JWT. ID ở path của API admin là **đối tượng bị tác động**, không thay thế danh tính admin.
- Admin API kiểm tra claim và role/status hiện tại trong DB; token cũ không cho phép admin đã bị khóa/hạ quyền tiếp tục mutation.
- Read API dùng DTO, AsNoTracking, chọn field cần thiết; không serialize entity/password/token hash.
- Các list mới phân trang: `page=1`, `pageSize=20`, tối đa 100; input ngoài giới hạn trả 400. Sort cố định, thêm Id để thứ tự ổn định.
- Response list thống nhất `{ items, page, pageSize, totalCount }`. Filter enum sai trả 400; filter không có kết quả trả 200 với items rỗng.
- Thời gian input ISO 8601 có offset; so sánh và lưu UTC. Dùng TimeProvider để test ranh giới thời gian.
- Điểm giữ JSON number như `/me` hiện tại, tài liệu phải nhắc FE về giới hạn số nguyên an toàn của JavaScript. Không đổi toàn bộ kiểu trả về âm thầm.
- API mới có mã lỗi máy đọc được (`code`) để FE không phải so chuỗi tiếng Anh. Chuẩn hóa dần bằng ProblemDetails có `code`, `traceId`; giữ tương thích hoặc ghi rõ thay đổi với lỗi cũ `{ message }`.
- 400: input sai; 401: JWT không hợp lệ; 403: quyền/trạng thái không cho phép; 404: không thấy tài nguyên hoặc không được xem tài nguyên riêng; 409: xung đột nghiệp vụ; 429: giới hạn request; 500/503: lỗi nội bộ/hạ tầng phù hợp.
- Không trả SQL, connection string, stack trace hoặc dữ liệu bí mật trong response Production.

## 5. Chặng 1 — Cấp điểm và sổ lịch sử điểm

### API đề xuất

| Method/path | Input | Output chính |
|---|---|---|
| POST `/api/admin/users/{userId}/points/grants` | `{ "amount": 500, "reason": "Điểm tham gia sự kiện" }` | 201: transactionId, userId, amount, balanceAfter, performedBy, reason, createdAt |
| GET `/api/users/me/point-transactions` | page, pageSize, type tùy chọn | Lịch sử ví của người gọi |
| GET `/api/admin/users/{userId}/point-transactions` | page, pageSize, type tùy chọn | Lịch sử của user đích; ADMIN ACTIVE |

### Công việc

- Thêm contract/service/controller vừa đủ; reuse DbContext, TimeProvider, cách kiểm tra actor hiện có.
- Validate amount nguyên dương, reason trim không trắng và <= 500 ký tự; dùng checked arithmetic để tránh tràn long khi cộng số dư.
- Kiểm tra user đích và ví tồn tại; không tự tạo ví để che dữ liệu thiếu nhất quán.
- Mặc định triển khai chỉ cấp cho USER ACTIVE; user bị BLOCKED trả 409, user/ ví thiếu xử lý rõ ràng. Đây là lựa chọn bổ sung cần ghi trong API docs.
- Một transaction gồm kiểm tra/khóa user liên quan, khóa ví, cộng balance, cập nhật updatedAt, thêm PointTransaction ADMIN_GRANT và AuditLog POINTS_GRANTED.
- performedBy lấy từ JWT; amount lưu dương, balanceAfter là số dư ngay sau lần cộng; userVoucherId null.
- Nếu ghi audit/lịch sử lỗi, balance phải rollback. Không có endpoint sửa balance trực tiếp.
- Lịch sử sắp xếp createdAt giảm dần + id, có amount có dấu, balanceAfter, type, reason, createdAt và voucher ID khi có.
- Không có API sửa/xóa point transaction.

**Idempotency cấp điểm:** scope hiện tại chưa có khóa request cho ADMIN_GRANT. Mỗi POST thành công là một lần cấp mới; FE không tự retry khi timeout, admin kiểm tra lịch sử trước khi gửi lại. Ghi hạn chế này rõ trong docs. Nếu muốn bảo đảm retry tự động cho grant, phải thống nhất thêm contract/cột unique trước; không dùng cache hoặc suy đoán trùng amount/reason để deduplicate.

### Nghiệm thu

- USER gọi grant nhận 403; token admin cũ sau block/demotion bị từ chối.
- Cấp 500 rồi 200 cho ví 0: balance 700, hai transaction + hai audit đúng actor/reason.
- amount 0/âm/tràn số và reason trắng bị từ chối, không thay đổi dữ liệu.
- Hai admin cấp đồng thời vào một ví: tổng balance chính xác, không mất update.
- User không đọc được lịch sử của user khác bằng sửa query/path.

## 6. Chặng 2 — API đọc campaign và tra cứu cho admin

### API đề xuất

| Method/path | Quyền | Filter/input |
|---|---|---|
| GET `/api/campaigns` | Public | page, pageSize; phase UPCOMING/ONGOING/ENDED tùy chọn |
| GET `/api/campaigns/{campaignId}` | Public | Guid; trả campaign và offers |
| GET `/api/admin/campaigns` | ADMIN ACTIVE | page, pageSize, status, từ khóa tên tùy chọn |
| GET `/api/admin/users` | ADMIN ACTIVE | page, pageSize, email, status tùy chọn |

### Rule và output

- Public chỉ đọc campaign PUBLISHED; Draft/Cancelled không xuất hiện, detail trả 404. Admin vẫn xem được mọi trạng thái.
- Campaign đã kết thúc vẫn có thể được xem; phase tính theo `now < startsAt`, `startsAt <= now < endsAt`, `now >= endsAt`.
- Không lưu UPCOMING/ONGOING/ENDED vào status DB; response tách `status` quản trị và `phase` hiển thị.
- Public detail trả id, tên/mô tả, lịch, quota và offer id/name/description/pointsCost/totalQuantity/issuedQuantity/remainingQuantity/expiresAt.
- `remainingQuantity = totalQuantity - issuedQuantity` chỉ hỗ trợ hiển thị; không cam kết lượt đổi sau đó thành công.
- Không public email admin, actor ID không cần thiết, audit hay mã voucher.
- Danh sách admin users chỉ có id/email/displayName/role/status/balance/timestamp cần thiết; email filter normalize, giới hạn độ dài tìm kiếm.
- Chưa thêm endpoint list offers public riêng nếu detail đã đủ; tránh hai contract trùng mục đích.

### Nghiệm thu

- Không lộ Draft/Cancelled qua list/detail public; admin list filter chính xác.
- Kiểm tra đúng ranh giới start/end với đồng hồ test.
- Phân trang ổn định, giới hạn kích thước, không N+1 query.

## 7. Chặng 3 — Đổi voucher: phần cốt lõi

### Contract đề xuất

`POST /api/voucher-offers/{offerId}/redeem`, JWT của tài khoản USER ACTIVE.

Header bắt buộc: `Idempotency-Key: <UUID do client sinh cho một lần bấm đổi>`.

Không body. Server lấy user từ JWT; giá, campaign, hạn và tồn kho đọc DB. Chuẩn hóa UUID về cùng một dạng trước lưu vào `request_key`; không nhận userId/pointsCost/code/expiresAt do client tự gửi.

- Client tạo key mới khi chủ động đổi thêm một voucher.
- Retry cùng thao tác phải gửi lại cùng key; không sinh key mới mỗi lần retry.
- Thành công lần đầu: 201, gồm id voucher, offerId, campaignId suy ra từ offer, code, pointsSpent, claimedAt, expiresAt.
- Retry đã thành công: 200, trả voucher đã phát; không phát/trừ thêm. Response lõi lấy từ snapshot trong user_vouchers, không lấy giá mới hoặc số dư hiện tại để giả làm kết quả cũ.
- Nếu có `Location`, trỏ endpoint GET voucher của chính người gọi.
- Thiếu/sai key: 400 `INVALID_REQUEST_KEY`.
- Cùng user + cùng key nhưng offer khác: 409 `IDEMPOTENCY_KEY_REUSED`.
- Hết hàng: 409 `OUT_OF_STOCK`; thiếu điểm: 409 `INSUFFICIENT_POINTS`; hết quota: 409 `CAMPAIGN_QUOTA_EXCEEDED`.
- Chưa bắt đầu/đã kết thúc/cancel/chưa publish: 409 với code riêng; offer không tồn tại: 404; user BLOCKED: 403.
- Những lần thất bại rollback không lưu key thành công. Cùng key có thể thử lại sau khi được cấp điểm, miễn chưa từng phát thành công.

### Quy ước lock dùng chung

Mục tiêu: tuần tự hóa thao tác trên cùng ví, cho phép nhiều user cạnh tranh tồn kho, và xác định rõ kết quả khi cancel/block chạy đồng thời.

Thứ tự khi một transaction cần các tài nguyên này: **user rows → wallet → campaign → offer**. Có thể bỏ qua tầng không cần, nhưng không đi ngược thứ tự.

- Nếu cần nhiều user row (admin + user đích), sắp xếp Guid theo cùng một quy ước trước khi lock. Xác định chế độ lock mạnh nhất cần cho mỗi row từ đầu, tránh nâng FOR SHARE lên FOR UPDATE giữa chừng.
- Redeem khóa user FOR SHARE để việc đổi status phải chờ; rồi ví FOR UPDATE.
- Grant khóa actor/target theo thứ tự chung; sau đó ví FOR UPDATE.
- Block khóa actor/target theo thứ tự chung; target FOR UPDATE. Chỉ khóa USER, không cho tự khóa admin hoặc khóa admin khác trong endpoint MVP này.
- Campaign mutation giữ actor FOR SHARE → campaign FOR UPDATE như hiện có.
- Redeem dùng campaign FOR SHARE; cancel/update campaign dùng FOR UPDATE. Không khóa độc quyền toàn campaign cho mọi lượt đổi nếu shared lock đủ.
- Conditional UPDATE của offer tự lấy row lock cần thiết; cập nhật offer sau campaign lock.
- Rà thêm lock gián tiếp từ FK/SaveChanges; test race thực tế. Quy ước trên là thiết kế phải được chứng minh bằng test, không phải bảo đảm tuyệt đối không bao giờ deadlock.

### Luồng transaction cần triển khai

1. Parse userId từ JWT, offerId và key; bắt đầu transaction trên PostgreSQL.
2. Khóa user và xác nhận USER ACTIVE; khóa ví của user. Nếu thiếu ví, báo dữ liệu không nhất quán, không tự sửa bằng redeem.
3. Tìm user_vouchers theo `(user_id, request_key)` sau khi đã có wallet lock. Nếu tồn tại, kiểm tra offer và trả kết quả cũ. BLOCKED vẫn bị chặn; campaign đã hết hạn/cancel không làm mất kết quả thành công trước đó của user còn ACTIVE.
4. Đọc offer để biết campaignId; khóa campaign FOR SHARE, sau đó đọc lại dữ liệu offer cần dùng dưới transaction. Không dựa vào tracked entity cũ hoặc cache.
5. Lấy thời gian hiện tại **sau khi chờ lock**, kiểm tra PUBLISHED và `startsAt <= now < endsAt`. Quy ước thời điểm hợp lệ là lúc kiểm tra này; không yêu cầu commit phải xảy ra trước endsAt.
6. Đếm tất cả user_vouchers của user JOIN voucher_offers trong campaign. Tính cả voucher đã hết hạn; không lọc theo offer hiện tại. Wallet lock khiến các lượt đổi khác của cùng user phải chờ.
7. Kiểm tra quota và balance >= pointsCost.
8. Tăng issued_quantity bằng UPDATE có điều kiện `issued_quantity < total_quantity`, đúng offer/campaign, cập nhật updatedAt; số row ảnh hưởng phải bằng 1. Nếu 0 thì rollback và trả hết hàng hoặc lỗi tương ứng đã xác định.
9. Trừ điểm bằng dữ liệu ví đã lock, updatedAt mới; có thể thêm điều kiện balance >= cost làm lớp bảo vệ bổ sung.
10. Sinh code ngẫu nhiên đủ mạnh, độ dài <= 64, có unique index. Không dùng mã tuần tự dễ đoán; không coi code là giấy phép xem thông tin user khác.
11. Tạo UserVoucher với snapshot pointsSpent/expiresAt và key; tạo PointTransaction VOUCHER_REDEMPTION amount = -pointsSpent, balanceAfter đúng, performedBy=userId, reason do server đặt; ghi audit VOUCHER_REDEEMED.
12. Save và commit toàn bộ. Nếu bất kỳ bước nào lỗi thì rollback tồn kho, ví, voucher, lịch sử và audit.
13. Sau commit mới invalidation cache và gửi realtime nếu đã làm. Cache/realtime lỗi không được báo rằng DB đã rollback; log lỗi phụ và để client retry cùng key nhận lại voucher.

### Các điểm dễ sai phải kiểm tra

- Dùng isolation có hành vi đã kiểm chứng, ưu tiên Read Committed + row lock rõ ràng cho MVP; mỗi request dùng DbContext riêng.
- Raw SQL luôn parameterized. Không ghép offerId/key từ request vào SQL text.
- ExecuteUpdate/raw UPDATE không tự cập nhật entity EF đã tracked. Tránh SaveChanges ghi đè issued_quantity/balance bằng snapshot cũ; chọn một cách cập nhật hoặc reload có chủ đích.
- Unique violation làm transaction PostgreSQL hiện tại lỗi: rollback trước khi truy vấn lại. Phân biệt constraint tên code với constraint key, không biến mọi unique violation thành retry thành công.
- Nếu retry do code collision/deadlock, dùng transaction/DbContext mới, số lần giới hạn, chỉ khi biết transaction cũ đã rollback; không retry mù mọi lỗi.
- Nếu mất kết nối ngay lúc commit, kết quả có thể chưa rõ: client retry cùng key để tra kết quả, không tự phát thêm với key khác.
- Cancel giữ exclusive lock trước: lượt redeem mới thấy CANCELLED và thất bại. Redeem giữ shared lock trước: nó có thể commit, cancel chờ rồi dừng các lượt sau. Quy tắc tương tự áp dụng block user.
- Không thêm campaignId vào user_vouchers cho MVP: JOIN qua offer hiện tại đủ kiểm tra quota. Nếu tối ưu sau này phải thiết kế constraint giữ campaignId và offerId nhất quán.

### Voucher của user

| Method/path | Input | Rule |
|---|---|---|
| GET `/api/users/me/vouchers` | page, pageSize, expired tùy chọn | Chỉ voucher của người gọi; sắp xếp claimedAt giảm dần + id |
| GET `/api/users/me/vouchers/{id}` | Guid | Lọc đồng thời id + userId; không thuộc mình trả 404 |

Trả code chỉ cho chủ sở hữu; kèm pointsSpent, claimedAt, expiresAt, tên offer/campaign cần hiển thị. Tính isExpired theo thời gian; không thêm USED/REDEEMED state hay API sử dụng voucher tại cửa hàng. Cancel campaign không xóa voucher đã phát.

## 8. Chặng 4 — Khóa user và API giám sát cho admin

| Method/path | Input | Output/rule |
|---|---|---|
| POST `/api/admin/users/{userId}/block` | `{ "reason": "Lạm dụng hệ thống" }` | 200 id/status/updatedAt; ADMIN ACTIVE, target USER |
| GET `/api/admin/user-vouchers` | page, pageSize, userId, campaignId, offerId, from, to tùy chọn | Lịch sử phát; không trả code mặc định |
| GET `/api/admin/audit-logs` | page, pageSize, actorUserId, action, entityType, entityId, from, to tùy chọn | Lịch sử audit; kiểm tra quyền trong DB |

- Block phải validate reason không trắng <= 500; transaction cập nhật status/updatedAt và audit USER_BLOCKED chứa lý do + trước/sau.
- Block user đã BLOCKED trả trạng thái hiện tại, không ghi audit trùng. User không tồn tại 404; target ADMIN bị từ chối theo rule rõ ràng.
- JWT đã phát không tự biến mất khi block; redeem phải kiểm tra DB trong transaction, không chỉ tin claim.
- Giữ hành vi đọc `/me` và dữ liệu của chính mình bằng JWT còn hạn nếu BLOCKED; login và mutation bị chặn. Ghi rõ lựa chọn này trong docs/test.
- Chưa thêm unblock hoặc đổi role HTTP: không nằm trong scope đã chốt.
- Audit không chứa password, token, secret, body auth hoặc code voucher. Không mở endpoint sửa/xóa audit.
- Khoảng thời gian filter dùng UTC; from bao gồm, to không bao gồm; kiểm tra from < to khi có cả hai.
- Lịch sử phát dựa vào user_vouchers, không tạo bảng lịch sử đổi thứ hai.

## 9. Chặng 5 — Hoàn thiện auth, lỗi, rate limit và cache

### Auth/email/cleanup

- Validate options khi startup: connection/JWT cần thiết, issuer/audience không rỗng, TTL dương, retention/cooldown hợp lệ, SMTP và VerificationPageUrl hợp lệ. Không log secret khi báo cấu hình lỗi.
- Giới hạn độ dài token verify theo format token đang sinh để tránh nhận chuỗi quá lớn.
- Kiểm tra email trim/lowercase nhất quán trước kiểm tra trùng; hiện DataAnnotations chạy trước service nên cần xác định rõ trường hợp email có khoảng trắng và test contract đó.
- Bổ sung BackgroundService đơn giản dọn pending quá retention, interval cấu hình được, có CancellationToken và scope DbContext mỗi vòng. Không cần scheduler framework.
- Cleanup chỉ xóa pending quá retention; không xóa user/ví. Kiểm tra race cleanup/verify/resend; hết TTL token và hết retention là hai khái niệm khác nhau.
- Định nghĩa rõ đăng ký lại pending cũ: không gửi token sắp bị cleanup theo createdAt cũ; chọn reset vòng đăng ký hết retention trong transaction và test trước khi thay đổi.
- SMTP vẫn gửi sau commit; thất bại không tạo tài khoản chính thức. Có log đã lọc bí mật, thông báo có thể gửi lại sau cooldown; không giữ transaction DB trong lúc chờ SMTP.
- Mailpit chỉ local. File cấu hình mẫu production dùng placeholder, không có account/secret thật. Việc chọn provider và xác minh gửi ra hộp thư thật thuộc giai đoạn deploy sau.

### HTTP và logging

- Thêm exception handler/ProblemDetails cho lỗi bất ngờ; log nội bộ với traceId, response Production không lộ stack trace.
- Phân biệt business conflict và lỗi hạ tầng; không catch mọi exception rồi trả 400 hoặc 200.
- Bổ sung `code` ổn định, cập nhật API docs và test cho cả lỗi controller/validation; quyết định rõ body 401/403/404 thay vì FE đoán.
- Chuyển rate-limit numbers cần chỉnh thành config; giữ auth limits, thêm giới hạn redeem theo userId đã xác thực; anonymous auth limit theo IP.
- Nếu rate limit đọc JWT/userId, đặt middleware authentication trước limiter và test thứ tự middleware.
- Trả 429 với thông tin retry khi limiter cung cấp được; rate limit không thay transaction/unique constraint.
- Không log Authorization header, token verify trên URL, password, secret SMTP/JWT/DB hoặc mã voucher.

### HybridCache

- Cache DTO public campaign/list/offer; TTL cấu hình, mặc định khoảng 60 giây cho thông tin ít đổi.
- Phase phải tính theo thời gian hiện tại hoặc bảo đảm TTL không làm sai ranh giới start/end.
- Nếu cache stock, TTL riêng 1–2 giây; không đặt stock chung object 60 giây rồi quảng bá là số lượng realtime. Có thể đọc stock trực tiếp trước để đơn giản.
- Cache key gồm filter/page/pageSize, normalize input; tránh tạo vô hạn key từ input tùy ý.
- Sau commit publish/cancel/update/create offer: invalidate detail và list liên quan. Sau redeem: invalidate stock liên quan.
- Kiểm tra race cache miss đang nạp dữ liệu cũ trong lúc invalidation: dùng cơ chế phù hợp của thư viện hoặc TTL ngắn, ghi rõ mức trễ chấp nhận được và test. Không hứa mọi view luôn lập tức đồng bộ với DB.
- Không cache quyết định redeem, trạng thái actor cho mutation, balance/quota dùng để trừ điểm hoặc kết quả có code giữa các user.
- Một instance dùng memory cache. Tài liệu nêu rõ invalidation memory không tự lan sang instance khác; Redis là mở rộng sau.

## 10. Chặng 6 — Kiểm thử và chuẩn bị vận hành local

### Bộ test cần có

Thêm project test trong `backend/tests` nếu chưa có; chọn bộ công cụ đơn giản phù hợp repo. Test lock/concurrency phải dùng PostgreSQL thật trong môi trường test riêng, không EF InMemory/SQLite để kết luận về row lock PostgreSQL.

| Nhóm | Trường hợp bắt buộc | Điều cần chứng minh |
|---|---|---|
| Registration | Trùng email khác case, verify đồng thời, token hết hạn/cũ/đã dùng, resend cooldown | Một user + một ví, token không tái sử dụng |
| Auth | Sai password, BLOCKED, JWT thiếu/hết hạn/sai chữ ký, USER gọi admin | HTTP và quyền đúng |
| Campaign | Draft edit, thiếu offer publish, sai expiry/quota, publish đôi, cancel lặp | Rule/audit đúng, không sửa cấu hình đã publish |
| Grant | Hai admin cùng cấp, overflow, reason sai, audit insert lỗi | Không mất update; balance/history/audit nguyên tử |
| Redeem cơ bản | Thành công, thiếu điểm, hết hàng, sai thời gian, cancel, BLOCKED | Đúng code/status và DB không đổi khi thất bại |
| Quota campaign | A + A + B đủ quota 3; lượt tiếp theo A/B đều thất bại | Quota toàn campaign, cho đổi cùng offer nhiều lần |
| Idempotency | Cùng key/cùng offer đồng thời; cùng key/khác offer; user khác cùng key | Một thành công logic, không trừ/phát lại; key theo user |
| Stock race | Nhiều user đủ điểm tranh 1 hoặc 10 voucher | Tổng thành công đúng tồn kho, issued không vượt |
| Balance race | Một user có điểm chỉ đủ một lượt, gửi nhiều key | Không âm; chỉ số lượt đủ điểm thành công |
| Quota race | Cùng user đổi nhiều offer, quota 1/3, điểm và stock dư | Tổng thành công đúng quota |
| Cancel/block race | Chạy song song với redeem có kiểm soát thứ tự lock | Kết quả phù hợp giao dịch nào có lock trước |
| Rollback | Cố tình fail sau tăng stock/trừ điểm, trước commit | Tất cả bảng trở về trạng thái trước request |
| Privacy | Sửa voucher ID/user ID, public đọc Draft | Không truy cập dữ liệu người khác/ẩn |
| Cache | Warm cache rồi publish/cancel/redeem; phase qua start/end | Độ trễ hiển thị theo contract; redeem luôn đọc DB |

Yêu cầu khi viết test:

- Request song song dùng connection/DbContext riêng và barrier khi cần, không chỉ gọi tuần tự thật nhanh.
- Dùng TimeProvider hoặc dữ liệu thời gian tương đối; không hard-code lịch đã qua khiến test tự hỏng sau vài tháng.
- Kiểm tra DB sau test: issued bằng số voucher phát, mỗi voucher đúng một debit, debit đúng pointsSpent, balance bằng tổng signed amount với ví bắt đầu 0; audit không trùng khi replay.
- Test retry sau response thất lạc bằng gọi cùng key sau commit; thêm tình huống mất kết nối nếu harness hỗ trợ.
- Test fault injection ở test harness; không thêm endpoint public cố tình gây lỗi.
- Dataset ngẫu nhiên có prefix, cleanup chỉ dữ liệu test trong DB test; không xóa toàn bộ DB/volume của người dùng.
- Có một script smoke test end-to-end và tập test concurrency lặp lại được; báo số request/thành công/xung đột/thời gian thực đo, không tự tuyên bố chịu tải lớn nếu chưa đo.

### Schema và migration

- Đọc snapshot + history DB trước tạo migration; không tạo lại tám bảng nếu đã tồn tại.
- Thêm index chỉ khi query mới hoặc đo đạc chứng minh cần: kiểm tra plan cho quota JOIN, danh sách voucher/lịch sử và filter admin trên dữ liệu có quy mô hợp lý.
- Không thêm unique `(user_id, voucher_offer_id)`; không bỏ check/FK để làm test pass.
- Nếu thay schema: tạo migration đúng project, đọc Up/Down và SQL, liệt kê tác động dữ liệu trước khi database update. Không tự drop bảng, reset volume hoặc viết lại migration đã áp dụng.
- Không mặc định chạy migration khi API Production startup. Chuẩn bị lệnh/script chạy có kiểm soát sau này.

### Chuẩn bị chạy và bàn giao, chưa deploy thật

- Xóa WeatherForecast/Class1 template không dùng sau khi xác nhận không có reference; cập nhật docs/API inventory.
- CORS cấu hình allowlist frontend local và placeholder domain production; test preflight, không mở wildcard chỉ để hết lỗi.
- Health endpoints tách liveness (process chạy) và readiness (DB truy cập được), không lộ config. SMTP không nhất thiết là dependency khiến mọi API bị unready.
- Cấu hình forwarded headers chỉ tin proxy đã cấu hình, chuẩn bị HTTPS/reverse-proxy để tránh redirect loop và IP spoofing ảnh hưởng limiter.
- Cấu hình request/DB timeout hợp lý, truyền CancellationToken. Không giữ transaction qua SMTP/cache/network bên ngoài.
- Tạo Dockerfile multi-stage và .dockerignore cho API; giữ Compose local và volume hiện có. Không đưa User Secrets, .env thật hoặc mật khẩu vào image.
- Có config mẫu/env variable inventory: DB, JWT, SMTP, verification URL, CORS, cache TTL, cleanup interval, rate limit; startup fail rõ nếu thiếu thông tin bắt buộc.
- Ghim version package đang dùng thay wildcard khi chốt baseline; không nâng framework/provider ngoài phạm vi chỉ vì tiện sửa file.
- Build Release, chạy smoke test container local và Production environment với secrets local được truyền an toàn; ghi rõ test nào chưa chạy được.
- Hướng dẫn backup/restore PostgreSQL và migration rehearsal trên DB dùng thử; không chạy restore đè DB thật. Có checklist giữ volume và không dùng `docker compose down -v` để khắc phục lỗi thông thường.
- Bootstrap admin bằng script có audit, tài khoản riêng đã verify; không seed password admin cố định hoặc public route promote admin.
- README hướng dẫn từ máy sạch: prerequisites, restore tool/dependency, config secrets, Compose, migration, chạy/test, Mailpit/Postman.
- Cập nhật `docs/api-reference.md`, OpenAPI response metadata và Postman collection/environment template không chứa credential/token thật.
- Tài liệu kiến trúc ngắn: tám bảng, luồng đăng ký/đổi, lock order, quota/idempotency, lý do cache không quyết định giao dịch.

## 11. SignalR — tùy chọn sau core

- Hub có auth cho thông báo riêng; không tin userId client gửi để join group người khác.
- Group campaign chỉ nhận số lượng/campaign ID; code voucher chỉ gửi chủ sở hữu.
- Emit sau commit; lỗi emit không làm giao dịch đã commit thành thất bại.
- Reconnect gọi API đọc lại; DB là nguồn chuẩn, chấp nhận mất notification.
- Chưa thêm outbox/queue. Nếu không đủ thời gian thì bỏ chặng này và ghi rõ chưa triển khai.

## 12. Điều kiện hoàn thành backend MVP

- [x] Có đủ auth, profile, admin campaign/offer, public campaign, grant, redeem, voucher/history, block và admin audit/history API trong scope.
- [x] Quota toàn campaign; repeat cùng offer bằng key khác hoạt động; retry cùng key không tạo giao dịch mới.
- [x] Mọi thay đổi balance có point transaction và audit thích hợp cùng transaction.
- [x] Không vượt stock/quota, không âm balance; test race trên PostgreSQL chứng minh được.
- [x] Cancel/block phối hợp với redeem bằng DB lock; token cũ không vượt được kiểm tra mutation.
- [x] Không lộ dữ liệu riêng/secret; validation, lỗi và quyền có contract/test.
- [x] Cache chỉ đọc, TTL/invalidation và giới hạn một instance được tài liệu hóa.
- [x] Build Release + test chính + smoke test end-to-end pass; ghi bằng chứng và hạn chế thực tế.
- [x] Migration được kiểm tra, schema không bị mở rộng ngoài scope; môi trường test tách khỏi dữ liệu người dùng.
- [x] Có Dockerfile, config mẫu, health, CORS và hướng dẫn chạy Production local.
- [x] API docs/Postman/README khớp code, không chứa token/password thực tế.

Không được đánh dấu hoàn thành chỉ vì API trả 200 trong một lượt test Postman. SignalR, Redis và gửi email thật trên hosting không phải điều kiện hoàn thành code MVP ở giai đoạn này.

## 13. Công việc để sau khi backend local được nghiệm thu

1. Chọn nhà cung cấp và kiểm tra lại giá/điều kiện hiện tại. Backend phải chạy liên tục, không chọn gói sleep/cold start làm phương án đáp ứng yêu cầu.
2. Tạo môi trường production, secrets riêng, PostgreSQL bền vững, backup và quyền DB phù hợp.
3. Cấu hình SMTP provider thật, sender/domain verification và DNS email theo yêu cầu provider; test gửi/verify qua hộp thư thật.
4. Cấu hình HTTPS, domain, trusted proxy, CORS và verification URL trỏ frontend thật.
5. Áp dụng migration đã review, bootstrap admin, smoke test production bằng dữ liệu kiểm thử được xác định rõ.
6. Theo dõi lỗi/latency/tài nguyên, xác nhận backup khôi phục được và có phương án rollback phiên bản.

Frontend vẫn là phần riêng. Backend hoàn thành không có nghĩa website đã có giao diện cho người dùng cuối; trước khi frontend có trang verify, luồng local vẫn test token qua Mailpit/Postman.

## 14. Prompt triển khai gốc — đã thực hiện

Lưu lại yêu cầu từng dùng để triển khai backend. Không dùng khối này để bắt đầu lại từ đầu; khi làm tiếp, đọc source và mục 13 để xử lý phần hosting còn lại.

```text
Bạn đang làm VoucherHub tại D:\VoucherHub. Hãy hoàn thiện backend MVP trước deploy theo docs/backend-completion-plan.md.

Đầu tiên kiểm tra cwd, đọc AGENTS.md nếu có, đọc toàn bộ tài liệu kế hoạch, code hiện tại, migrations và docs/api-reference.md. Tài liệu là baseline; đối chiếu trạng thái thật, không viết lại phần đã hoàn thành. Giữ mọi thay đổi của người dùng.

Thực hiện các chặng 0–6 theo phụ thuộc, chia thành bước nhỏ dễ kiểm tra. Mỗi bước implement, build/test phù hợp, cập nhật API docs và báo bằng tiếng Việt: đã làm gì, vì sao, cách test. Không dừng chỉ để hỏi có tiếp tục không khi vẫn còn việc trong scope đã được giao. Nếu gặp blocker cần thông tin thực sự hoặc quyền ngoài scope, mô tả cụ thể phần đã làm và điều đang thiếu.

Giữ monolith bốn project và đúng tám bảng nghiệp vụ. Không tự thêm merchant/category/inventory/redemption/counter/refresh-token table hoặc generic repository/CQRS/MediatR. SignalR chỉ làm sau core nếu được yêu cầu; Redis/queue/outbox chưa bắt buộc.

Ưu tiên admin grant + point history, public/admin list, rồi redeem + user vouchers, block user/admin history, cuối cùng hardening/cache/tests và chuẩn bị vận hành local. Routes và lựa chọn mặc định trong kế hoạch là contract triển khai đề xuất; có thể điều chỉnh nhỏ theo code hiện tại nhưng phải cập nhật docs và giải thích, không mở rộng tính năng.

Quota theo toàn campaign, cho phép user đổi cùng offer nhiều lần; user_vouchers đã là lịch sử đổi. Redeem dùng PostgreSQL transaction/row locks, consistent lock order, conditional stock update, wallet lock, unique (user_id, request_key). Phối hợp cancel/block với redeem. Đảm bảo mỗi thành công có đúng một voucher, debit và audit trong cùng transaction; retry trả kết quả cũ. Sau commit mới invalidate cache hoặc emit realtime.

Read APIs phải có phân trang và kiểm tra ownership. Admin mutation kiểm tra role/status trong DB, userId người gọi lấy JWT. Không tin giá, quota, balance hoặc tồn kho từ client/cache. Không log password/token/code voucher/secret.

Test concurrency trên PostgreSQL thật trong DB test riêng, gồm stock/balance/quota/idempotency/cancel/block và rollback. Không dùng EF InMemory để kết luận lock đúng. Không xóa dữ liệu người dùng hoặc reset Docker volume. Nếu test bị chặn bởi môi trường, báo rõ, không tuyên bố pass.

Nếu cần migration, đọc Up/Down và SQL, trình bày tác động trước database update; không có thao tác mất dữ liệu ngoài scope. Không sửa migration đã áp dụng chỉ để thay đổi lịch sử.

Chuẩn bị README, API reference, Postman template, Dockerfile, config mẫu, CORS, health và hướng dẫn Production local. Không tạo host, mua dịch vụ, gửi email thật hoặc deploy production trong tác vụ này. Không đưa secret thật vào source/docs/image.

Kết thúc bằng checklist công việc đã hoàn thành, test đã chạy/kết quả, phần còn thiếu và mẫu request để tôi tự kiểm tra. Chỉ đánh dấu backend MVP hoàn thành khi đạt các tiêu chí bắt buộc trong tài liệu.
```
