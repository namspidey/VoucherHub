# VoucherHub — Đề xuất tái cấu trúc backend và frontend

> Cập nhật triển khai 09/10/2026: cấu trúc BE/FE đã được refactor theo các ưu tiên trong kế hoạch. Xem [Kiến trúc sau refactor](architecture-guide.md) để biết cấu trúc thực tế, trách nhiệm, luồng dữ liệu, kết quả test và các mục vẫn để dành cho giai đoạn sau. Phần đánh giá dưới đây được giữ như baseline trước refactor.

Ngày đánh giá: 09/10/2026. Workspace: D:\VoucherHub.

Đây là bản thiết kế và kế hoạch triển khai dựa trên source hiện tại, không phải xác nhận đã refactor hay đạt production-ready. Phạm vi lần này chỉ tạo tài liệu; không thay đổi code, schema hoặc dữ liệu.

## 1. Quyết định kiến trúc

**Đề xuất: monolith tổ chức theo module nghiệp vụ, giữ 4 project backend; frontend React tổ chức theo feature.** Giữ PostgreSQL làm nơi đảm bảo transaction và tính đúng của flash sale.

Không có một cấu trúc thư mục duy nhất là “chuẩn mọi công ty”. Với VoucherHub, chất lượng nên được đánh giá bằng: dễ tìm use case, ít phụ thuộc chéo, contract rõ, thay đổi nhỏ kiểm thử được, vận hành có số liệu và không mất tính đúng khi tải tăng.

| Quyết định | Đề xuất cho VoucherHub | Lý do |
|---|---|---|
| Backend | ASP.NET Core + EF Core + PostgreSQL, một deployment | Transaction đổi voucher đi qua nhiều bảng; giữ cùng DB đơn giản và đáng tin cậy |
| Phân lớp | Domain / Application / Infrastructure / Api | Tận dụng project hiện tại, làm rõ trách nhiệm thay vì viết lại |
| Module | Identity, Campaigns, Wallets, Vouchers, Audit | Chia theo nghiệp vụ, không chia thành service mạng |
| Frontend | React + Vite + JavaScript, feature-based | Giữ lựa chọn JS của người dùng; page không còn gánh fetch, form, auth và UI cùng lúc |
| UI | Bootstrap là hệ class chính, Bootstrap Icons; thu hẹp Tailwind | UI hiện chủ yếu dùng Bootstrap và đã có xung đột class collapse |
| Dữ liệu FE | TanStack Query cho dữ liệu server; Context cho session | Giảm fetch/effect lặp, quản lý loading, stale data và invalidation |
| Kiểm thử | PostgreSQL integration tests + FE behavior tests + E2E trọng yếu | Lint/build không chứng minh transaction hoặc trải nghiệm đúng |
| Scale | Đo bottleneck rồi tăng năng lực; chuẩn bị chạy nhiều instance | Không mặc định microservices, Kubernetes, Redis hay message broker |

Microsoft mô tả monolith và Clean Architecture là các lựa chọn tổ chức ứng dụng phù hợp tùy bối cảnh; monolith vẫn có thể tách trách nhiệm và dependency rõ ràng. Đây là cơ sở tham khảo, không phải bằng chứng mọi công ty dùng cùng mô hình. [Tài liệu kiến trúc Microsoft](https://learn.microsoft.com/en-gb/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)

### Các ràng buộc giữ nguyên

- Chỉ 8 bảng MVP đã thống nhất; không tự thêm merchants, voucher_redemptions, refresh_tokens hoặc outbox.
- Quota thuộc toàn campaign. Một user được đổi cùng offer nhiều lần trong quota.
- user_vouchers vừa là voucher đã phát vừa là lịch sử đổi.
- Không có mua điểm, sử dụng voucher thực tế, hoàn điểm hoặc thu hồi voucher.
- Cache không quyết định số dư, quota hay tồn kho lúc đổi.
- Không rewrite migration đã áp dụng; không xóa DB để phục vụ đổi thư mục.
- Giữ API hiện hữu trong các bước refactor cấu trúc; thay contract phải có bước chuyển tiếp rõ ràng.
- Backend production phải đáp ứng yêu cầu không ngủ khi không có request.

## 2. Đánh giá source hiện tại

Đã đối chiếu controller, DTO/interface, DbContext, các service redeem/grant/block/campaign và các trang FE/auth/API client. Có script test trong scripts; chưa có thư mục backend/tests trong lần kiểm tra này. Không tìm thấy AGENTS.md qua tìm kiếm workspace. Lần đánh giá này không chạy lại suite test hay benchmark.

### Nền tảng nên giữ

- Backend đã có hướng project reference hợp lý.
- Redeem có PostgreSQL transaction, khóa user/ví/campaign, tăng tồn kho bằng UPDATE có điều kiện.
- Ví, voucher, point transaction và audit được ghi trong một transaction.
- Schema có check constraint, FK Restrict và unique index bảo vệ dữ liệu.
- Có TimeProvider, paging, health endpoints, rate limit và HybridCache.
- scripts/test-backend-core.ps1 đã chứa case tranh tồn kho, replay, quota, block/cancel race; scripts/test-backend-isolated.ps1 tạo DB test riêng. Không nên bỏ các test này khi đưa vào test framework.
- FE đã có route public/user/admin và nối phần lớn endpoint.

### Vấn đề cần ưu tiên

Các mục “nguy cơ” dưới đây được suy ra từ code, chưa phải kết quả tái hiện bằng browser/load test trong lần đánh giá này.

| Mức | Vị trí hiện tại | Nhận xét và tác động | Hướng xử lý |
|---|---|---|---|
| P0 | CampaignDetailPage.jsx | Mỗi lần bấm redeem sinh UUID mới; retry sau mất response có thể thành lượt đổi mới | Giữ key theo một ý định đổi, dùng lại khi kết quả chưa rõ |
| P0 | VerifyEmailPage.jsx + main.jsx | POST verify trong effect; StrictMode bật nên development có thể gửi lại, success/error ghi đè nhau | Mutation do người dùng xác nhận, không tự POST khi mount |
| P0 | lib/api.js | Chỉ nhận application/json, bỏ qua application/problem+json | Parse JSON theo media type, chuẩn hóa lỗi |
| P0 | lib/api.js và RedemptionService.cs | FE dùng OFFER_SOLD_OUT/USER_QUOTA_EXCEEDED trong khi BE trả OUT_OF_STOCK/CAMPAIGN_QUOTA_EXCEEDED | Đồng bộ error code; thêm contract test |
| P1 | Infrastructure/*Service.cs | Phần lớn nghiệp vụ nằm trong Infrastructure; Application chủ yếu interface/record | Chuyển orchestration từng use case sang Application với port persistence cụ thể |
| P1 | ICampaignService và các controller khác | Kiểm tra admin dùng nhờ service campaign | Tách current-user/policy dùng chung; vẫn kiểm tra trong transaction khi cần lock |
| P1 | VoucherHubDbContext.cs | Mapping 8 entity trong một file | Tách IEntityTypeConfiguration, xác minh model không đổi |
| P1 | Program.cs | Wiring, options, auth, limiter, CORS cùng một file | Tách extension theo chức năng và options validation |
| P1 | pages/* | Lặp fetch/loading/error/pagination, nhiều request chưa bảo vệ race khi đổi filter | Query hooks theo feature, query key đủ tham số |
| P1 | AuthContext.jsx | Lỗi profile bị nuốt thành null; login có thể điều hướng dù chưa tải được profile | Phân biệt anonymous / authenticated / unavailable |
| P1 | AdminUsersPage.jsx | Sau grant chọn lại object selected cũ nên số dư panel có thể cũ | selectedUserId + dữ liệu query mới, không lưu bản sao object |
| P1 | main.jsx/index.css | Bootstrap JS toàn bộ + Tailwind reset/utilities; đã phải vá visibility | Một chủ sở hữu UI/state; giảm chồng framework |
| P1 | FE/forms/API numbers | Number có thể mất chính xác với bigint | Chốt contract điểm và giới hạn rõ ràng |
| P2 | AuthShell/FormField trong LoginPage.jsx | Page khác import component từ page login | Chuyển component dùng chung về đúng feature/shared |
| P2 | App.jsx và admin pages | Import eager, editor nhiều trách nhiệm | Route lazy + component nhỏ theo workflow |
| P2 | Assets/App.css cũ | Template còn tồn tại, cần xác nhận reference trước dọn | Xóa phần chắc chắn không dùng trong PR riêng |

React xác nhận StrictMode chạy thêm chu kỳ effect trong development và hướng dẫn bảo vệ việc fetch trước response về sai thứ tự. Do đó không giải quyết bằng tắt StrictMode hoặc trì hoãn effect chỉ để qua lint. [React useEffect](https://react.dev/reference/react/useEffect)

## 3. Backend: kiến trúc mục tiêu

### 3.1. Trách nhiệm và dependency

- **Domain:** entity, trạng thái, rule thuần; không HTTP, EF, SMTP, cache.
- **Application:** use case, command/query model, kết quả nghiệp vụ, interface tích hợp cần thiết. Không phụ thuộc Infrastructure.
- **Infrastructure:** EF mapping, migration, SQL lock, persistence adapter, SMTP, JWT signing, cache adapter.
- **Api:** HTTP route, model binding, validation hình dạng input, authentication, policy, ánh xạ kết quả sang HTTP và DI.

Dependency compile-time:

~~~text
Api ------------> Application ----------> Domain
 |                       ^
 +--> Infrastructure ----+
             |
             +--------------------------> Domain
~~~

Api tham chiếu Infrastructure để đăng ký implementation tại composition root. Controller không dùng DbContext hoặc implementation cụ thể. Mỗi module là nhóm code trong bốn project; chưa tạo thêm năm bộ project/module riêng.

### 3.2. Cây thư mục gợi ý

~~~text
backend/
  src/
    VoucherHub.Domain/
      Identity/           User, PendingRegistration, trạng thái
      Campaigns/          Campaign, VoucherOffer, rule lịch/quota
      Wallets/            PointWallet, PointTransaction
      Vouchers/           UserVoucher
      Audit/              AuditLog
    VoucherHub.Application/
      Common/
        Pagination/
        Security/
        Errors/
      Identity/
        Register/
        VerifyEmail/
        Login/
        GetProfile/
        BlockUser/
        Ports/
      Campaigns/
        CreateCampaign/
        UpdateCampaign/
        PublishCampaign/
        CancelCampaign/
        SaveOffer/
        ListCampaigns/
        Ports/
      Wallets/
        GrantPoints/
        GetPointHistory/
        Ports/
      Vouchers/
        RedeemVoucher/
        GetMyVouchers/
        Ports/
      Audit/
        SearchAuditLogs/
    VoucherHub.Infrastructure/
      Persistence/
        VoucherHubDbContext.cs
        Configurations/   một configuration/entity
        Migrations/       giữ migration lịch sử
        Identity/
        Campaigns/
        Wallets/
        Vouchers/
        Audit/
      Identity/           JWT, password hashing adapter nếu cần
      Email/
      Caching/
      BackgroundJobs/
      DependencyInjection.cs
    VoucherHub.Api/
      Controllers/        giữ controller, không cần đổi Minimal API
      Contracts/
      Security/
      Errors/
      Configuration/
      Program.cs
  tests/
    VoucherHub.Domain.Tests/
    VoucherHub.IntegrationTests/
~~~

Một use case chỉ cần các file đủ phục vụ nó, ví dụ RedeemVoucher.cs, RedeemVoucherCommand.cs, RedeemVoucherResult.cs. Không bắt buộc mỗi endpoint có handler/interface/validator/mapper riêng nếu chưa có giá trị.

### 3.3. Di chuyển nghiệp vụ mà không làm vỡ transaction

Đích đến: Application orchestration, Infrastructure thực thi persistence. Tránh interface CRUD generic hoặc trả IQueryable ra khỏi persistence adapter.

Ví dụ cho redeem:

- RedeemVoucher nhận userId do server xác định, offerId, requestKey.
- IRedemptionSessionFactory tạo session gắn với một transaction/DbContext scoped.
- Session cung cấp thao tác cụ thể: khóa user, khóa ví, đọc replay, khóa campaign, đếm quota, tăng issued có điều kiện, lưu voucher/ledger/audit, commit.
- Application kiểm tra trạng thái và điều phối các bước trên.
- Dispose khi chưa commit phải rollback; adapter không tự commit từng repository.
- Không thêm transaction thứ hai bên trong helper hay gọi HTTP trong transaction.

Đây là port theo use case, không phải “repository cho mọi entity”. Chỉ tách khi integration test đã bảo vệ. Nếu interface mới chỉ lặp từng dòng EF mà khó đọc hơn, giữ atomic persistence routine trong Infrastructure trong giai đoạn chuyển tiếp, đưa rule thuần ra trước và ghi rõ phần còn lại.

Đọc catalog/list đơn giản có thể qua query interface trả DTO projection. Không hydrate toàn bộ aggregate chỉ để đọc bảng.

### 3.4. Domain rule và validation

- Request validation: thiếu field, độ dài, số dương, format.
- Domain/use case: Draft mới sửa được, hạn offer sau endsAt, trạng thái active, quota campaign.
- DB: unique/FK/check là hàng rào cuối cho dữ liệu.
- Dùng constants hoặc enum mapping string cho trạng thái; refactor không được vô tình đổi dữ liệu ADMIN/USER, ACTIVE/BLOCKED.
- Đưa rule có ý nghĩa vào phương thức như Publish/Cancel hoặc policy thuần; không tạo base entity/repository tổng quát chỉ để giảm vài dòng.
- Rule phụ thuộc trạng thái cạnh tranh phải đánh giá sau lock; validation trước transaction không thay được bước này.

### 3.5. Bảo toàn hợp đồng lock

Trình tự redeem hiện tại cần ghi thành invariant trong code và test:

1. Begin transaction.
2. User FOR SHARE; kiểm tra USER/ACTIVE.
3. Wallet FOR UPDATE.
4. Tìm replay bằng (user_id, request_key); khác offer thì conflict.
5. Campaign FOR SHARE; kiểm tra published và thời gian.
6. Đếm quota toàn campaign, kiểm tra số dư.
7. UPDATE offer có điều kiện issued_quantity < total_quantity.
8. Trừ ví, ghi user_voucher, point_transaction, audit.
9. Commit; sau đó invalidate cache.

Lưu ý hành vi hiện tại: blocked user bị chặn trước bước replay. Refactor phải giữ hành vi này hoặc có quyết định đổi contract rõ ràng.

Grant/block khóa các user liên quan theo cùng thứ tự ID rồi mới khóa ví khi cần. Publish/cancel phải phối hợp khóa campaign tương thích với redeem. Không sửa thứ tự lock của một flow độc lập với các flow còn lại.

Nếu triển khai retry cho deadlock/serialization failure: retry toàn transaction, số lần hữu hạn, tạo lại state tracked, giữ cùng idempotency key. Không tự retry grant sau mất response vì grant hiện chưa có idempotency contract.

### 3.6. DB và migration

- Tách mapping bằng ApplyConfigurationsFromAssembly.
- Sau refactor thuần mapping: kiểm tra EF model không có thay đổi pending; nếu sinh migration ngoài dự kiến phải điều tra, không apply.
- Giữ unique (user_id, request_key); không biến index (user_id, voucher_offer_id) thành unique.
- Giữ unique nullable user_voucher_id trong point_transactions: đảm bảo tối đa một giao dịch liên kết mỗi voucher; transaction/test đảm bảo thực sự có bản ghi tương ứng.
- Các index phục vụ quota/history phải được xác nhận bằng EXPLAIN trên dữ liệu gần thực tế trước khi bổ sung.
- Cân nhắc (wallet_id, created_at, id), (user_id, claimed_at, id) cho truy vấn phân trang nếu đo cho thấy cần; không thêm hàng loạt index theo phỏng đoán.
- Migration schema thực sự phải có kiểm tra SQL, backup/restore và kế hoạch tương thích phiên bản trước/sau.

## 4. Contract API thống nhất

Giữ các path hiện có. Đổi folder không phải lý do đổi URL sang /v2 hoặc tạo thêm BFF.

### Error contract đề xuất

~~~json
{
  "type": "about:blank",
  "title": "Không đủ điểm",
  "status": 409,
  "code": "INSUFFICIENT_POINTS",
  "traceId": "<request-trace-id>"
}
~~~

Validation có thêm errors theo field. FE dùng code để dịch thông báo; không so sánh nguyên văn tiếng Anh.

Chuyển tiếp: FE đọc được cả message/code cũ và ProblemDetails mới trước khi BE chuẩn hóa. Các 401/403/404 rỗng cũng phải xử lý được. Không trả stack trace, SQL hoặc secret cho client.

### Dữ liệu và tương thích

- Thời gian: ISO 8601 UTC; input datetime-local phải chuyển múi giờ rõ ràng.
- Pagination giữ items/pageNumber/pageSize/totalCount. Chuyển cursor chỉ với endpoint có nhu cầu đo được.
- Điểm: khuyến nghị contract string thập phân cho các trường long có thể vượt safe integer. UI hiển thị bằng xử lý chuỗi/BigInt, không Number(value) vô điều kiện.
- Việc chuyển number → string là thay đổi contract: cập nhật OpenAPI, FE parser, request binding BE và test đồng thời; không chỉ sửa DTO response.
- Bước ngắn hạn: chặn input vượt Number.MAX_SAFE_INTEGER và hiển thị lỗi rõ nếu response vượt giới hạn; không âm thầm làm tròn.
- UUID luôn là string; actor/user sở hữu lấy từ JWT, không tin body client.
- Xuất OpenAPI làm nguồn contract; tài liệu và Postman cập nhật cùng PR.
- JavaScript có thể dùng JSDoc cho DTO và checkJs ở các module API trước; TypeScript là nâng cấp sau nếu nhóm thống nhất, không bắt buộc rewrite hiện tại.

## 5. Frontend: kiến trúc mục tiêu

### 5.1. Cấu trúc feature-based

~~~text
frontend/src/
  app/
    router.jsx
    providers.jsx
    config.js
    layouts/
      PublicLayout.jsx
      AccountLayout.jsx
      AdminLayout.jsx
  features/
    auth/
      api.js
      queries.js
      session.js
      components/
        AuthShell.jsx
        LoginForm.jsx
        RegistrationForm.jsx
      pages/
    campaigns/
      api.js
      queries.js
      components/
        CampaignCard.jsx
        OfferCard.jsx
        CampaignForm.jsx
        OfferForm.jsx
      pages/
        CatalogPage.jsx
        CampaignDetailPage.jsx
        AdminCampaignListPage.jsx
        AdminCampaignEditorPage.jsx
    redemption/
      api.js
      useRedeemVoucher.js
      pendingRequests.js
      components/
        RedeemConfirmation.jsx
        RedemptionResult.jsx
    wallet/
      api.js
      queries.js
      components/
        PointHistoryTable.jsx
        GrantPointsForm.jsx
      pages/
    vouchers/
      api.js
      queries.js
      components/
      pages/
    users/
      api.js
      queries.js
      components/
      pages/
    audit/
      api.js
      queries.js
      pages/
  shared/
    api/
      httpClient.js
      ApiError.js
      errorMessages.js
    ui/                   Button, Field, Dialog, Alert, EmptyState
    lib/                  formatDate, formatPoints
    styles/               tokens.css, base.css
  test/
~~~

Quy tắc import:

- app ghép feature; feature dùng shared.
- shared không import feature hoặc page.
- Page cùng feature dùng component nội bộ; page không là thư viện component.
- Dùng API công khai nhỏ của feature khi cần cross-feature.
- Không tạo global utils/components chứa tất cả.
- Tách file khi có trách nhiệm riêng; tránh bẻ một form nhỏ thành quá nhiều file.

### 5.2. Tách ba loại state

| Loại state | Ví dụ | Nơi quản lý |
|---|---|---|
| Server state | Campaign, user profile, voucher, point history | TanStack Query |
| Session | Có session hay không, hành động login/logout, quá trình bootstrap | Auth provider/session module |
| UI/form state | Modal mở, input đang sửa, filter chưa submit | State cục bộ hoặc form library khi cần |
| URL state | Page, phase, bộ lọc đã áp dụng | Search params để back/refresh giữ trạng thái |

Profile nên có một nguồn dữ liệu server duy nhất; không giữ một bản trong Context và một bản khác trong query cache. AdminUsers chọn selectedUserId, lấy object mới từ query.

Query key cần có user ID với dữ liệu riêng tư, ID entity và filter:
- ['campaigns', 'public', filters]
- ['campaign', id]
- ['me', userId]
- ['vouchers', userId, filters]
- ['point-history', userId, filters]
- ['admin-users', filters]

Sau redeem: invalidate profile, voucher cá nhân, lịch sử điểm, catalog/detail liên quan. Logout/đổi tài khoản: hủy request đang chạy và xóa cache riêng tư trước khi hiển thị tài khoản mới.

Cấu hình retry tường minh: mutation đăng ký/verify/grant/redeem không auto-retry; GET không retry 401/403/404/validation. GET lỗi mạng/5xx có thể retry hữu hạn. TanStack Query có default retry cho query nên cần chủ động cấu hình, không dùng mặc định một cách vô thức. [TanStack Query — Query retries](https://tanstack.com/query/latest/docs/framework/react/guides/query-retries)

### 5.3. Redeem và trạng thái kết quả chưa rõ

Một lần bấm xác nhận đổi là một ý định nghiệp vụ. Tạo key một lần và lưu theo user + offer + operation:

~~~text
Xác nhận -> tạo key -> gửi request
  thành công 201/200 -> hiển thị voucher -> đóng operation
  conflict xác định -> hiển thị lý do -> kết thúc operation
  mất kết nối/timeout/5xx -> kết quả chưa rõ -> thử lại với key cũ
~~~

- Không sinh UUID mới khi người dùng bấm “Thử lại” cho operation chưa rõ.
- Không cho mở lượt đổi mới cùng offer khi operation cũ chưa được xử lý rõ.
- Có thể lưu operation pending trong sessionStorage để reload cùng tab vẫn giữ key; không chứa token/mật khẩu.
- Disable double click chỉ hỗ trợ UX; DB unique + transaction mới bảo vệ dữ liệu.
- Mất response sau commit vẫn có thể nhận lại voucher nhờ replay.
- Không optimistic decrement số dư/tồn kho như thể giao dịch đã thành công.
- Khi xác nhận thành công nhưng refresh profile lỗi: giữ thông báo voucher thành công, báo riêng phần số dư chưa cập nhật.
- Grant chưa có key: nếu mất response, hướng dẫn kiểm tra lịch sử trước khi gửi lại; bổ sung idempotency grant là đề xuất nghiệp vụ/schema riêng.

### 5.4. Auth và bảo mật

- Phân biệt session hết hạn, sai thông tin đăng nhập, blocked user và API tạm không truy cập được.
- Một request cũ trả 401 không được xóa session mới vừa login; gắn request với phiên/token đã dùng.
- Login endpoint dùng chế độ public, không tự gắn token cũ.
- Sau login chỉ điều hướng khi profile tải thành công, hoặc có màn hình retry profile.
- Đồng bộ logout nhiều tab qua storage event; hết hạn giữa phiên phải cập nhật UX.
- Route guard chỉ hỗ trợ điều hướng; backend vẫn quyết định quyền.
- Verify page hiển thị bước xác nhận rồi POST trong submit handler; không tự POST từ effect. Có form resend cho link sai/hết hạn.
- Không đưa token xác thực vào telemetry hoặc URL của request tới bên thứ ba; có thể xóa query token khỏi thanh địa chỉ sau khi xử lý phù hợp.

JWT hiện lưu localStorage nên JavaScript cùng origin đọc được. Giữ làm bước tương thích ngắn hạn, ghi rõ giới hạn. Phương án tăng bảo vệ cần ADR riêng: cookie HttpOnly/Secure với same-site deployment/proxy phù hợp và CSRF protection. Cookie không tự giải quyết XSS/CSRF, và FE/BE khác site cần xét chính sách cookie thực tế. Không tự thêm refresh token hay bảng session vào đợt refactor cấu trúc.

### 5.5. UI dễ maintain

- Bootstrap utility/component là nền tảng chính vì source đang dùng nhiều nhất; giữ Bootstrap Icons.
- Gỡ Tailwind khi đã thay toàn bộ class cần thiết, hoặc tạm prefix utilities và kiểm soát reset trong giai đoạn chuyển tiếp.
- Tránh duy trì hai bộ global styles và thêm !important để vá liên tục.
- CSS riêng tập trung ở design tokens, branding và component thực sự đặc thù.
- Dùng React state hoặc React-compatible component cho modal/collapse; không để Bootstrap JS và React cùng quản lý một DOM/state.
- Thay window.confirm/prompt bằng dialog có label, focus, keyboard navigation.
- Form liên kết label/htmlFor, hiện lỗi cạnh field, loading có aria-live, icon-only button có accessible name.
- Voucher code dài cần wrap; kiểm tra mobile, copy thành công/thất bại, bảng có scroll hợp lý.
- Empty, error, loading, refreshing và no-permission là trạng thái riêng.
- Không hiển thị ưu đãi 50K hoặc “mỗi ngày” như dữ liệu thật nếu chỉ là trang trí; nội dung marketing phải phù hợp dữ liệu.
- Lazy-load nhóm admin; không cần SSR cho MVP admin/ứng dụng hiện tại.

### 5.6. Thư viện: thêm khi có trách nhiệm rõ

| Công cụ | Thời điểm | Mục đích |
|---|---|---|
| TanStack Query | Khi tách API hooks | Cache/request lifecycle, mutation và invalidation |
| Bootstrap + Bootstrap Icons | Giữ | UI nhất quán, ít CSS tự viết |
| React Router | Giữ | Layout/route/lazy loading |
| React Hook Form | Khi form editor phức tạp hoặc validation lặp | Form state; chưa bắt buộc cho login đơn giản |
| Zod hoặc schema validator tương đương | Nếu cần runtime contract validation rõ | Không cài song song nhiều bộ validation |
| Vitest + Testing Library + MSW | Bước kiểm thử FE | Behavior + mô phỏng network theo contract |
| Playwright | Bước E2E | Luồng UI thực sự và responsive |
| Redux/Zustand | Chưa cần | Chưa thấy client state đủ phức tạp để biện minh |

Không “hạn chế JavaScript” bằng cách bỏ xử lý trạng thái nghiệp vụ. Hạn chế JS trang trí; vẫn cần code rõ cho auth, retry, lỗi và accessibility.

## 6. Kiểm thử và tiêu chí chất lượng

### Backend

Giữ script smoke hiện có trong lúc bổ sung xUnit integration tests với PostgreSQL thật (DB cô lập hoặc Testcontainers). EF InMemory không mô phỏng PostgreSQL row lock/check constraint đủ để chứng minh concurrency.

Các case bắt buộc:

1. N user đủ điểm tranh K voucher, quota đủ: đúng K voucher, không issued vượt total.
2. Một user đổi nhiều offer cùng campaign: tổng thành công không vượt quota.
3. Cùng key/cùng offer đồng thời: một lần trừ điểm, một voucher, replay trả cùng ID.
4. Cùng key/khác offer: conflict, không thêm debit.
5. Cùng user tranh ví giữa nhiều campaign: không âm.
6. Grant/redeem đồng thời: balance và ledger nhất quán.
7. Block/cancel cạnh tranh redeem: kết quả phù hợp thứ tự lock; không phát mới sau thao tác chặn đã hoàn tất.
8. Inject lỗi trước commit: không sót thay đổi stock/wallet/voucher/ledger/audit.
9. Giả lập commit thành công nhưng response mất: retry key cũ không tạo lần thứ hai.
10. Boundary startsAt/endsAt, expired verification token và quyền ownership.

Assert DB theo fixture: issued_quantity bằng số voucher thực phát; mỗi voucher có đúng một debit với amount = -points_spent và đúng ví; balance cuối = balance đầu + tổng amount.

Test concurrency phải có barrier hoặc cơ chế cho request chồng lấn, không chỉ gửi tuần tự. Tách 429 rate limit khỏi 409 nghiệp vụ. Nếu tăng rate limit để đo DB, chỉ override ở môi trường test riêng và kiểm tra rate limit production bằng suite khác.

### Frontend và E2E

- HTTP client hiểu ProblemDetails, body rỗng, network failure, AbortError.
- Không ghi đè filter mới bằng response cũ.
- Verify không gửi hai mutation khi mount/StrictMode.
- Redeem retry giữ key; đổi mới sau kết quả đã rõ mới có key mới.
- Logout/đổi user không hiển thị voucher của user trước.
- Grant cập nhật số dư panel; lỗi không làm mất input.
- E2E: register → email test → verify → login; admin tạo/publish → cấp điểm → user đổi → history.
- Browser QA ở mobile và desktop: navbar, bảng, form, token hết hạn, keyboard focus.

Load test Node/k6 kiểm tra tải và latency là bước bổ sung; không thay thế assertion DB. Dùng fixture riêng và cleanup có kiểm soát.

## 7. Vận hành và scale có điều kiện

### Trước deploy thật

- Lockfile npm, tool manifest .NET; CI dùng phiên bản được chốt và npm ci.
- Environment validation: production thiếu API URL không fallback localhost; không có secret trong VITE_*.
- CI: build BE, integration tests, FE lint/test/build, E2E trọng yếu khi môi trường cho phép.
- Build artifact/container một lần, đẩy cùng artifact qua staging/production.
- Migration chạy bằng bước release riêng, không để nhiều API replica đồng thời auto-migrate.
- Secret từ môi trường/secret store; không hardcode trong compose hoặc parse password bằng regex từ YAML trong test lâu dài.
- Reverse proxy trust đúng IP; rate limit IP không tin X-Forwarded-For tùy ý.
- HTTPS, SMTP thật, CORS domain thật, SPA rewrite cho deep link.
- Backup định kỳ và kiểm thử restore; rollback application không mặc định downgrade DB.
- Log có trace ID và outcome; không ghi JWT/password/verification token/voucher code.
- Theo dõi latency p95/p99, 5xx, redeem outcome, lock wait, deadlock, connection pool và SMTP failure.
- Health live/ready không công khai chi tiết nhạy cảm. SMTP hỏng cần cảnh báo riêng.

### Khi tải tăng

| Dấu hiệu đã đo | Hướng xử lý trước |
|---|---|
| Catalog đọc nhiều, DB CPU cao | Tối ưu projection/index/pagination; cache đúng TTL và invalidation |
| Một offer nóng gây lock wait | Giữ transaction ngắn, giới hạn concurrency/admission, kiểm soát connection pool |
| API CPU/instance quá tải | Tăng tài nguyên hoặc replica sau khi rà soát state dùng chung |
| Nhiều API instance | Cache invalidation liên instance, rate limit tổng, cleanup job coordination, SMTP/secret nhất quán |
| Offset paging sâu chậm | Cursor pagination ở endpoint có nhu cầu, chuyển contract có kế hoạch |
| SMTP lỗi thường xuyên, mất email không chấp nhận được | Đề xuất durable delivery/outbox riêng; cần thống nhất thêm schema |
| Nhóm/module cần deploy độc lập thật | Khi đó mới đánh giá tách service và chi phí distributed transaction |

Redis chỉ là ứng viên khi có nhu cầu distributed cache/limiter đo được. Redis không thay PostgreSQL để bảo vệ số dư/quota/tồn kho. Tag invalidation và local cache giữa replica cần xác minh theo thiết kế cache thực tế; không giả định thêm Redis tự đồng bộ mọi thứ.

Monolith chạy nhiều replica vẫn có thể scale; nhiều replica cùng tranh một stock row vẫn bị giới hạn tại điểm ghi đó. Không cam kết throughput trước khi có benchmark.

## 8. Lộ trình refactor theo PR nhỏ

Ước lượng cho một người: khoảng 8–12 ngày tập trung cho phần ưu tiên cao đến staging cơ bản; có thể lâu hơn khi vừa học vừa làm. Không coi đây là cam kết thời gian hay yêu cầu hoàn thành mọi nâng cấp trước portfolio demo.

| Bước | Phạm vi | Điều kiện hoàn thành |
|---|---|---|
| 0 — Baseline | Ghi API/schema hiện tại, chạy test cô lập, ghi kết quả | Có trạng thái pass/fail và fixture lặp lại được |
| 1 — Correctness FE | Verify, retry key, error code/parser, auth state, stale balance | Test tái hiện lỗi và pass sau sửa |
| 2 — Contract | ProblemDetails, mapping status/code, DTO và giới hạn điểm | FE cũ/mới có giai đoạn tương thích; contract tests pass |
| 3 — BE mapping/composition | Configurations, options, DI, security helper | API behavior giữ nguyên, EF model không drift |
| 4 — BE use cases | Di chuyển từng feature; redeem làm sau khi transaction test đủ | Application không reference Infrastructure; lock contract giữ nguyên |
| 5 — FE feature structure | Di chuyển file từng nhóm, query hooks, form/components | Routing, session, cache invalidation và UI behavior pass |
| 6 — UI consistency | Một hệ CSS chính, React quản lý interactive UI, accessibility | Mobile/desktop QA, không còn vá collapse bằng conflict global |
| 7 — Delivery | CI, E2E, benchmark baseline, observability, deploy checklist | Build artifact tái lập, readiness/restore/rollback được kiểm chứng |

Bước 1 có thể làm trước khi chọn query library. Refactor cấu trúc không trộn với thay toàn bộ UI và schema trong cùng PR.

### Bảng chuyển file tiêu biểu

| Hiện tại | Đích đề xuất |
|---|---|
| Infrastructure/Vouchers/RedemptionService.cs | Application/Vouchers/RedeemVoucher + persistence session adapter |
| Infrastructure/Users/WalletService.cs | Application/Wallets/GrantPoints + query history + adapter |
| Application/Campaigns/ICampaignService.cs chứa nhiều records | Tách contract/use case theo trách nhiệm; policy admin ra Common/Security |
| Persistence/VoucherHubDbContext.cs | Context ngắn + Configurations/*Configuration.cs |
| Api/Program.cs | Composition root + Configuration/DependencyInjection extensions |
| frontend/src/lib/api.js | shared/api/httpClient + error mapping + auth/session |
| frontend/src/pages/CampaignDetailPage.jsx | features/campaigns/pages + redemption hook/components |
| frontend/src/pages/admin/AdminCampaignEditorPage.jsx | Campaign editor + CampaignForm + OfferForm + query/mutation hooks |
| frontend/src/pages/LoginPage.jsx chứa AuthShell/FormField | auth/components + shared/ui/Field |
| frontend/src/pages/admin/AdminUsersPage.jsx | users page + wallet grant/history component + selectedUserId |
| frontend/src/index.css | shared/styles/tokens.css + base.css + component styles cần thiết |

## 9. Definition of Done

- [ ] Scope 8 bảng và toàn bộ invariant nghiệp vụ được giữ.
- [ ] Baseline script tests được bảo tồn; integration tests cạnh tranh chạy trên PostgreSQL thật.
- [ ] Một request đổi có retry không phát/trừ hai lần.
- [ ] API contract, error code và documentation nhất quán.
- [ ] Rule nghiệp vụ có owner rõ; transaction boundary không bị chia nhỏ.
- [ ] FE tách server state, session, UI state; không stale response/identity leakage.
- [ ] Không tự POST verify từ mount; không tắt StrictMode để che lỗi.
- [ ] UI một hệ class chính, responsive và keyboard-accessible.
- [ ] Build/lint/tests/E2E có kết quả ghi nhận; điểm chưa kiểm chứng được nêu rõ.
- [ ] Refactor không sinh migration ngoài dự kiến.
- [ ] Cấu hình production, secret, migration, backup, quan sát lỗi có hướng dẫn vận hành.
- [ ] Không tuyên bố “scale tốt” chỉ dựa vào folder; có số liệu load test và bottleneck được giải thích.

Ưu tiên triển khai đầu tiên là các lỗi correctness ở FE và test transaction. Cấu trúc đẹp có giá trị khi giúp giữ những hành vi này đúng qua các lần thay đổi.

