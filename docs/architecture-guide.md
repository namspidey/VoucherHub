# VoucherHub — Kiến trúc sau refactor

Ngày cập nhật: 09/10/2026. Tài liệu mô tả **code đã triển khai**, đối chiếu với `architecture-refactoring-plan.md`; không phải tuyên bố đã hoàn thành mọi hạng mục production trong kế hoạch.

## 1. Những gì giữ nguyên và thay đổi

Giữ monolith, 4 project BE, React + JavaScript và 8 bảng MVP. Không thêm repository tổng quát, MediatR, microservices, bảng mới hoặc migration mới. Route nghiệp vụ hiện có được giữ nguyên. Quota vẫn thuộc campaign; một user vẫn đổi được cùng offer nhiều lần trong quota.

Thay đổi chính:

- BE: di chuyển orchestration đăng ký/login/campaign/cấp điểm/đổi voucher/block về Application; Infrastructure thực hiện persistence, SQL lock, JWT và SMTP qua interface.
- BE: tách mapping 8 entity thành 8 `IEntityTypeConfiguration`; tách DI/configuration khỏi Program; kiểm tra quyền admin dùng `IUserAccess`, không dùng nhờ campaign service.
- FE: thay cấu trúc `pages/components/lib` tập trung bằng `app/features/shared`; page không import component từ page khác; route lazy-load.
- FE: TanStack Query quản lý dữ liệu server; Auth Context quản lý trạng thái phiên. Mỗi feature có API và query hooks riêng.
- UI: giữ Bootstrap và Bootstrap Icons; gỡ Tailwind cùng Vite plugin vì UI không sử dụng, tránh hai hệ reset/class chồng nhau. Không import Bootstrap JavaScript toàn bộ; React quản lý mở navbar, HTML dialog quản lý modal.
- Tính đúng: verify không còn POST tự động trong effect; retry redeem giữ key khi chưa biết kết quả; API client hiểu ProblemDetails và không để 401 của phiên cũ xóa phiên mới.

## 2. Cấu trúc backend

```text
backend/src/
├── VoucherHub.Domain/
│   ├── Identity/                 User, PendingRegistration
│   ├── Campaigns/                Campaign, VoucherOffer, CampaignRules
│   ├── Wallets/                  PointWallet, PointTransaction
│   ├── Vouchers/                 UserVoucher
│   └── Audit/                    AuditLog
├── VoucherHub.Application/
│   ├── Identity/
│   │   ├── Login/                AuthenticationService
│   │   ├── Registration/         RegistrationService
│   │   └── Ports/                persistence/password/token/email interfaces
│   ├── Campaigns/
│   │   ├── UseCases/             CampaignService
│   │   └── Ports/                ICampaignStore
│   ├── Wallets/
│   │   ├── GrantPoints/           WalletService
│   │   └── Ports/                IWalletSessionFactory, IPointHistoryQueries
│   ├── Vouchers/
│   │   ├── RedeemVoucher/         RedemptionService
│   │   └── Ports/                IRedemptionSessionFactory, IVoucherQueries
│   ├── Users/
│   │   ├── BlockUser/             AdminUsersService
│   │   └── Ports/                IAdminUserStore, IAdminUserQueries
│   └── Common/
│       ├── Pagination/           Page<T>
│       ├── Security/             IUserAccess
│       └── Persistence/          IWriteTransaction, IDatabaseReadiness
├── VoucherHub.Infrastructure/
│   ├── Persistence/
│   │   ├── Configurations/       8 EF mappings
│   │   ├── Identity/             user/login/registration/query adapters
│   │   ├── Campaigns/            CampaignStore, CampaignCatalog
│   │   ├── Wallets/              WalletSessionFactory, PointHistoryQueries
│   │   ├── Vouchers/             RedemptionSessionFactory, VoucherQueries
│   │   ├── Migrations/           migrations và snapshot hiện có
│   │   ├── VoucherHubDbContext.cs
│   │   ├── WriteTransaction.cs
│   │   └── DatabaseReadiness.cs
│   ├── Identity/                 password hashing, JWT signing/options
│   ├── Email/                    SMTP sender/options
│   ├── BackgroundJobs/           pending registration cleanup
│   └── DependencyInjection.cs
└── VoucherHub.Api/
    ├── Controllers/              HTTP endpoints
    ├── Contracts/                request models/DataAnnotations
    ├── Configuration/            ServiceConfiguration
    ├── Errors/                   ApiErrorResultFilter
    └── Program.cs                khởi động, middleware, endpoint wiring
```

Các interface/result DTO vẫn ở module tương ứng; không tạo thêm một project cho từng module. Một số dịch vụ vẫn gom vài thao tác liên quan, ví dụ CampaignService tạo/sửa/publish/cancel. Chưa cần một class cho mọi endpoint khi điều đó chỉ làm tăng số file.

### 2.1. Bốn lớp làm gì?

| Lớp | Nhiệm vụ | Được biết | Không nên biết |
|---|---|---|---|
| Domain | Entity và rule thuần | C# và dữ liệu nghiệp vụ | HTTP, EF, SMTP, React |
| Application | Điều phối use case, kiểm tra rule, trả kết quả nghiệp vụ | Domain và các port của chính Application | DbContext, Npgsql, controller, HTTP status |
| Infrastructure | Hiện thực port bằng EF/PostgreSQL/password/JWT/SMTP | Application, Domain và thư viện hạ tầng | UI; không làm chủ luồng đổi voucher |
| Api | Bind/validate request, auth, gọi use case, đổi kết quả sang HTTP | Application; Infrastructure tại nơi đăng ký DI | SQL lock trong controller, chỉnh Balance trực tiếp |

`CampaignRules` hiện mới chứa rule thuần về trạng thái/thời gian đổi. Entity phần lớn vẫn là entity đơn giản; refactor này không giả vờ đã biến chúng thành rich domain model. Rule cần truy vấn hoặc transaction nằm trong Application.

### 2.2. Dependency lúc compile và luồng chạy không giống nhau

```text
Compile-time:
Api ───────────────► Application ──► Domain
 └──► Infrastructure ──► Application (+ Domain qua reference)

Runtime khi đổi:
VouchersController
    └── IRedemptionService → RedemptionService [Application]
          └── IRedemptionSessionFactory [Application interface]
                 └── RedemptionSessionFactory [Infrastructure implementation]
                        └── VoucherHubDbContext → PostgreSQL
```

Application không import Infrastructure. Nó yêu cầu một **khả năng** qua interface; DI chọn class đáp ứng khả năng đó. Ví dụ `IRedemptionSession.TryIssueAsync` chỉ nói “thử phát một voucher”; Infrastructure biết SQL `UPDATE ... WHERE issued_quantity < total_quantity`.

Api reference Infrastructure để đăng ký implementation ở composition root, không để controller truy vấn EF. `ServiceConfiguration` đăng ký HTTP/auth/CORS/limiter/cache; `AddVoucherHubInfrastructure` đăng ký persistence/adapters/use cases. Đây là cách gom wiring nhỏ gọn, chưa tạo container hay framework DI mới.

### 2.3. Các class quan trọng và quan hệ

| Class | Gọi/implement gì? | Trách nhiệm |
|---|---|---|
| AuthenticationService | ILoginStore, IUserPasswordService, IAccessTokenIssuer | Chuẩn hóa email, kiểm tra password/status, quyết định login |
| LoginStore | ILoginStore → DbContext | Đọc user, lưu hash mới nếu rehash |
| JwtTokenIssuer | IAccessTokenIssuer | Ký JWT; secret/options không nằm trong Application |
| RegistrationService | IRegistrationStore, IPendingPasswordService, IVerificationEmailSender | Register/resend/verify, transaction user + wallet, gửi email sau commit |
| RegistrationStore | IRegistrationStore → DbContext | Advisory lock theo email, đọc/khóa pending, thao tác persistence |
| CampaignService | ICampaignStore, TimeProvider | Draft rules, offer expiry, publish/cancel, ghi audit |
| CampaignStore | ICampaignStore → DbContext | Actor/campaign locks, tracked entities, transaction adapter |
| CampaignCatalog | ICampaignCatalog → DbContext | Projection/paging cho danh sách và chi tiết đọc |
| WalletService | IWalletSessionFactory, IPointHistoryQueries | Cấp điểm, kiểm tra overflow, ledger + audit cùng transaction |
| RedemptionService | IRedemptionSessionFactory, IVoucherQueries, CampaignRules | Điều phối redeem/replay/quota/điểm/voucher/ledger/audit |
| AdminUsersService | IAdminUserStore, IAdminUserQueries | Block user và audit; chuyển tiếp truy vấn quản trị |
| UserAccess | IUserAccess → DbContext | Kiểm tra ADMIN/ACTIVE hiện tại, dùng chung cho controller |
| WriteTransaction | IWriteTransaction → EF transaction | SaveChanges + commit; translate unique conflict; rollback/dispose |
| VoucherHubDbContext | 8 DbSet, ApplyConfigurationsFromAssembly | EF unit of work, tập hợp mapping; không phải lớp nghiệp vụ |
| ApiErrorResultFilter | MVC result filter | Thêm ProblemDetails cho object lỗi, giữ `message`/`code` cũ |

Read service chỉ đọc/projection có thể được hiện thực trực tiếp ở Infrastructure sau interface Application, ví dụ UserProfileService/CampaignCatalog. Không tạo một wrapper Application không có logic chỉ để gọi thêm một tầng.

Một port ở đây có phạm vi theo use case, không phải `IRepository<T>` cho mọi entity. Session cho redeem/grant giữ cùng DbContext và transaction; entity ví được tracked, nên thay đổi Balance được lưu cùng các bản ghi mới.

### 2.4. EF mapping và schema

`VoucherHubDbContext.OnModelCreating` gọi `ApplyConfigurationsFromAssembly`. Ví dụ `PointWalletConfiguration` chịu trách nhiệm tên bảng/cột, bigint, default 0, check balance >= 0, unique user_id và FK Restrict.

Các entity được chuyển thư mục nhưng **giữ namespace `VoucherHub.Domain.Entities`** ở lần refactor này. Snapshot migration hiện ghi tên CLR đó; đổi namespace ngay có thể gây model diff không phục vụ nghiệp vụ. Thư mục mới phân loại module, namespace giữ tương thích có chủ đích, không phải quên sửa.

Không sửa migrations đã áp dụng. EF kiểm tra không có pending model change sau refactor; không chạy database update vào DB `voucherhub`. Integration test áp dụng migrations vào DB tạm riêng rồi dọn DB đó.

## 3. Cấu trúc frontend

```text
frontend/src/
├── main.jsx                       Bootstrap CSS/icons + providers/router
├── app/
│   ├── router.jsx                 route composition, lazy pages
│   ├── providers.jsx              QueryClientProvider + AuthProvider
│   ├── queryClient.js             chính sách cache/retry chung
│   ├── ProtectedRoute.jsx         route guard + unavailable/retry UI
│   └── layouts/AppLayout.jsx      navbar/footer/Outlet
├── features/
│   ├── auth/                      session, context, api, pages, AuthShell
│   ├── campaigns/                 api, query hooks, pages, forms/cards
│   ├── redemption/                mutation, pending request key, api
│   ├── wallet/                    point history API/query/page
│   ├── users/                     profile/admin users API/query/pages
│   ├── vouchers/                  owned/admin vouchers API/query/pages
│   ├── audit/                     audit API/query/page
│   ├── dashboard/                 admin overview composition
│   └── navigation/pages/          NotFoundPage
├── shared/
│   ├── api/                       httpClient, config, ApiError, error map,
│   │                              queryString, useResource
│   ├── lib/                       formatting, useUrlState
│   ├── ui/                        fields/dialog/pagination/alerts/states,
│   │                              AdminShell (khung trình bày quản trị)
│   └── styles/base.css            theme nhỏ, dùng Bootstrap là chính
└── test/api.test.mjs               tests thuần Node cho API/session/key
```

### 3.1. Từng nhóm làm gì?

| Nhóm/file | Trách nhiệm | Liên quan |
|---|---|---|
| app | Lắp ráp ứng dụng, route và providers | Import feature pages, không chứa SQL/logic redeem |
| feature/pages | Trang và state form/dialog/filter của người dùng | Query hooks/API của feature, shared UI |
| feature/api.js | Endpoint cụ thể, method/body/query/header | shared httpClient |
| feature/queries.js | Query key, params, signal và cache dữ liệu server | feature API, TanStack Query |
| useRedeemVoucher | Mutation đổi voucher và invalidation sau success | pendingRequests, redeem API, QueryClient |
| AuthProvider | Trạng thái anonymous/loading/unavailable/authenticated | session, auth API, query `me` |
| session.js | Token/expiry/session scope và sự kiện login/logout | AuthProvider, adapter của httpClient |
| httpClient | Fetch, Bearer, JSON/ProblemDetails, ApiError, abort | Nhận session adapter; không import auth feature |
| useResource | Wrapper query tương thích loading/error/refetch | TanStack Query; không tự fetch trong effect |
| shared/ui | Component trình bày dùng lại | Nhận props; không gọi API nghiệp vụ |

`AdminShell` có menu quản trị nhưng chỉ dựng giao diện và links; không biết entity, API hay session. `CampaignForm`/`OfferForm` nằm trong feature campaign vì chúng biết các field riêng của nghiệp vụ.

Chiều import: **app → features → shared**. Shared không import app/feature; feature không import app hay page khác. Dashboard được phép kết hợp API của các feature; quyền truy cập cross-feature này là có chủ đích, không bắt buộc mọi feature độc lập tuyệt đối.

### 3.2. Phân chia state

- **Server state:** campaign, offers, me, wallet history, vouchers, users, audit nằm trong QueryClient.
- **Session state:** token/expiry/session scope nằm trong auth/session; Auth Context công bố trạng thái phiên và profile từ query, không giữ một bản profile song song.
- **UI state:** input form, selected user ID, dialog mở/đóng nằm trong React state gần component sử dụng.
- **URL state:** page và một số filter như phase/type/expired được đồng bộ query string. Chưa chuyển toàn bộ filter quản trị sang URL.
- **Pending intention:** request key đổi chưa có kết quả rõ nằm trong sessionStorage, tách theo user + offer.

Private query keys có session scope để tránh dùng dữ liệu người trước cho người sau. Khi session đổi, AuthProvider hủy query và clear cache. HTTP 401 chỉ clear session nếu token của request vẫn là token hiện tại.

Campaign detail cũng remount phần tương tác theo ID campaign + session scope, nên dialog/thông báo chứa mã voucher không còn hiển thị sau khi logout hoặc chuyển tài khoản.

Query mặc định stale 10 giây; retry tối đa một lần cho lỗi network/server, không retry lỗi nghiệp vụ 4xx. Mutation không tự retry. Public catalog gọi API với `public: true`, không đính kèm Bearer của user.

## 4. Luồng dữ liệu end-to-end

### 4.1. Login rồi đọc profile

```text
LoginPage (email/password)
 → AuthProvider.login
 → authApi.login → httpClient → POST /api/auth/login
 → AuthController → AuthenticationService
 → LoginStore đọc User; UserPasswordService kiểm tra hash
 → JwtTokenIssuer ký token
 ← accessToken + expiresAtUtc
 → saveSession, đổi scope và xóa cache phiên cũ
 → query GET /api/users/me → UserProfileService join user/wallet
 ← profile vào QueryClient → Auth Context → navbar/ProtectedRoute/page
```

Nếu login đã cấp token nhưng profile lỗi network, UI không giả vờ đã tải được user; giữ trạng thái unavailable và cho retry. Logout ở FE xóa phiên/cache, không phải thu hồi JWT đã phát ở BE. Token vẫn lưu localStorage ở MVP: refactor này **không** tự chuyển sang cookie/refresh token.

### 4.2. Xem campaign

```text
HomePage / CampaignDetailPage
 → campaign query hook (key gồm ID/filter/page)
 → campaignApi → httpClient → CampaignsController
 → HybridCache đọc public catalog
 → khi miss: ICampaignCatalog / CampaignCatalog → EF projection → PostgreSQL
 ← DTO → cache BE → query cache FE → components
```

Cache BE public hiện TTL 2 giây vì response có số lượng còn lại. Cache là dữ liệu hiển thị, **không** kiểm tra quota/điểm/tồn kho để redeem. Hai cache khác nhau: HybridCache ở server; QueryClient ở browser. Controller invalidate tag BE sau commit; FE invalidate query sau mutation success. Hiện HybridCache wiring/invalidation vẫn ở API, chưa tách adapter cache riêng.

### 4.3. Admin cấp điểm

```text
AdminUsersPage chọn selectedUserId, nhập amount/reason
 → walletApi.grant → WalletsController
 → JWT/role check + IUserAccess
 → WalletService
    → begin transaction, khóa actor/target theo thứ tự ID
    → kiểm tra ADMIN/ACTIVE và target USER/ACTIVE
    → khóa wallet, kiểm tra amount/reason/overflow
    → tăng Balance + PointTransaction + AuditLog
    → commit
 ← receipt → FE refetch users, invalidate wallet history
```

Panel lấy user được chọn từ danh sách query mới, không giữ object user cũ. Cấp điểm chưa có idempotency key; mất response không nên tự bấm lại trước khi kiểm tra lịch sử. Việc thêm idempotency cho grant sẽ là thay đổi contract riêng, không lẫn vào đổi thư mục.

### 4.4. User đổi voucher — phần quan trọng nhất

```text
CampaignDetailPage → dialog xác nhận
 → useRedeemVoucher
 → pendingRequests lấy/sinh key cho user + offer
 → POST /api/voucher-offers/{offerId}/redeem + Idempotency-Key
 → VouchersController (user ID từ JWT, không từ body)
 → RedemptionService
    1. Begin transaction
    2. Khóa User FOR SHARE; kiểm tra USER/ACTIVE
    3. Khóa PointWallet FOR UPDATE
    4. Tìm (user_id, request_key)
       - cùng offer: trả voucher cũ
       - khác offer: conflict
    5. Đọc offer, khóa Campaign FOR SHARE, đọc lại offer
    6. Kiểm tra Published, thời gian, campaign quota, balance
    7. UPDATE offer có điều kiện issued_quantity < total_quantity
    8. Trừ Balance; thêm UserVoucher + PointTransaction + AuditLog
    9. SaveChanges và commit
 ← 201 mới / 200 replay
 → xóa pending key; invalidate me/campaign/wallet/vouchers
```

Khóa ví tuần tự hóa request của cùng user, nên đếm user_vouchers join voucher_offers theo campaign an toàn giữa các luồng redeem của user đó. Không thêm campaign counter hoặc unique user + offer. Khóa campaign phối hợp cancel/edit; conditional UPDATE bảo vệ tồn kho giữa nhiều user. Transaction bao tất cả thay đổi, dispose khi không commit rollback.

Sau lỗi network/5xx/429, FE giữ key để retry cùng ý định. Sau success hoặc lỗi client xác định khác 429, key được bỏ. Key mới nghĩa là một lượt đổi mới, không phải retry. Nếu nhiều tab tự tạo key khác nhau thì đó là nhiều ý định đổi; quota và ví trong DB vẫn là chốt bảo vệ.

Invalidation/refetch lỗi sau commit không biến thành lỗi mutation để người dùng vô tình tạo lượt đổi mới. Chưa có SignalR/outbox ở flow này.

### 4.5. Đăng ký và xác thực

Register lưu pending + token hash, email gửi sau commit. VerifyPage **chỉ gọi POST khi người dùng bấm xác nhận**, tránh effect mount/StrictMode tự gửi mutation hai lần. Verification use case tạo User và ví 0 rồi xóa pending trong cùng transaction. Background cleanup nằm trong Infrastructure; không đưa token/password vào audit.

## 5. Response lỗi sau refactor

Object lỗi nghiệp vụ qua MVC result filter được bọc thành ProblemDetails, giữ field `message` và `code` để tương thích FE/Postman; có status/title và traceId. ValidationProblemDetails giữ errors theo field. Các challenge/forbid body rỗng vẫn xử lý theo status; không tuyên bố mọi 401/403 có JSON.

`httpClient` nhận cả `application/json` và `application/problem+json`, chuyển thành `ApiError(message, status, code, fields)`. Error map dùng code thực BE, ví dụ `OUT_OF_STOCK`, `CAMPAIGN_QUOTA_EXCEEDED`. Abort giữ AbortError, không hiển thị thành lỗi mất mạng.

BE vẫn dùng long/bigint. FE giới hạn input số điểm trong vùng số nguyên chính xác của JavaScript và từ chối response chứa integer không an toàn. Đây là guard, chưa phải contract long-as-string toàn hệ thống; dữ liệu lớn sẽ báo lỗi thay vì hiển thị số đã làm tròn.

## 6. Cách thêm chức năng hoặc sửa lỗi

Ví dụ sửa rule đổi voucher:

1. Bắt đầu từ `Application/Vouchers/RedeemVoucher/RedemptionService.cs`, không bắt đầu từ SQL trong controller.
2. Rule chỉ cần dữ liệu truyền vào có thể tách Domain; rule cần đếm/lock dùng port/session.
3. Nếu cần SQL mới, bổ sung method vào port rồi hiện thực trong Infrastructure; giữ đúng lock order.
4. Nếu shape request/response đổi, cập nhật contract/controller, FE feature API/error map, API docs/Postman và tests cùng nhau.
5. Chạy isolated tests, đặc biệt stock/quota/replay/rollback; build không đủ chứng minh tính đúng.

Ví dụ thêm page lịch sử khác:

1. `features/<module>/api.js` mô tả endpoint.
2. `queries.js` định nghĩa key đầy đủ filter/page/session scope, truyền AbortSignal.
3. Page gọi hook, render shared states/components; tránh tự tạo useEffect fetch mới.
4. Mutation invalidate các query liên quan; không sửa số dư/tồn kho bằng phỏng đoán client.

## 7. Kiểm tra và kết quả

Chạy từ root bằng PowerShell:

```powershell
dotnet build VoucherHub.slnx -c Release
dotnet ef migrations has-pending-model-changes --project backend/src/VoucherHub.Infrastructure --startup-project backend/src/VoucherHub.Api --configuration Release --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-backend-isolated.ps1
node scripts/check-architecture.mjs
```

FE từ `D:\VoucherHub\frontend`; dùng npm.cmd để tránh ExecutionPolicy của npm.ps1:

```powershell
npm.cmd run lint
npm.cmd test
npm.cmd run check:architecture
npm.cmd run build
```

Production build cần `VITE_API_BASE_URL` cấu hình đúng (xem `.env.example`); biến VITE là public, không đặt JWT signing key/password DB vào đây.

Kết quả kiểm tra lần refactor:

- BE Release build: 0 warning, 0 error.
- EF: không thay đổi model so với migration cuối.
- PostgreSQL isolated tests: grant/catalog/redeem/replay, ledger invariants, stock/quota/key races, block/cancel races, audit và rollback đều PASS.
- Auth tests: normalize, login trước verify, expiry, resend rotation, concurrent verify và một ví đều PASS.
- FE lint và production bundle: PASS; route đã tách chunk lazy.
- FE 10 tests: public Bearer, ProblemDetails/error code, validation fields, old/current 401, abort, unsafe bigint, pending key scope/retry, lost response/5xx/429 và business rejection đều PASS.
- Architecture checker: PASS. Đây là guard import/using đơn giản, **không** phải analyzer chứng minh mọi dependency hoặc mọi rule kiến trúc.

Các tests FE hiện là unit/contract tests thuần, chưa có React rendering/E2E browser test. Integration BE vẫn dùng scripts đã có, chưa chuyển thành xUnit project. Không xem lint/build như bằng chứng mọi giao diện đã được kiểm thử bằng trình duyệt.

## 8. Phần kế hoạch chưa thực hiện trong lần này

Không đồng nhất “refactor cấu trúc đã xong” với “mọi ý tưởng scale/deploy đã xong”. Các hạng mục cần bước riêng:

- Playwright E2E và component/form tests đầy đủ; chuyển BE test scripts sang test project khi có lợi ích.
- CI pipeline, staging, restore drill, monitoring/load benchmark và chốt hosting.
- Cookie/BFF hoặc refresh token nếu thay auth; hiện vẫn JWT access token localStorage và login lại khi hết hạn.
- Long-as-string contract nếu thực sự cần số điểm lớn hơn Number.MAX_SAFE_INTEGER.
- Redis/distributed cache/limiter khi chạy nhiều instance; hiện các cache/limiter vẫn một instance, transaction DB vẫn bảo vệ redeem.
- Persist đầy đủ mọi filter vào URL, chuẩn hóa form validation sâu hơn, chia editor khi tính năng tiếp tục tăng.
- Cache abstraction riêng nếu cần; chưa tạo interface chỉ để bọc một dòng HybridCache.

Không tự triển khai tính năng ngoài MVP hay mua dịch vụ deploy trong lần refactor này.
