using VoucherHub.Application.Common.Pagination;
using VoucherHub.Domain.Entities;
namespace VoucherHub.Application.Vouchers.Ports;

// One session owns one database transaction; dispose without commit rolls back.
public interface IRedemptionSessionFactory
{
    Task<IRedemptionSession> BeginAsync(CancellationToken ct);
}
public interface IRedemptionSession : IAsyncDisposable
{
    Task<User?> LockUserAsync(Guid userId, CancellationToken ct);
    Task<PointWallet?> LockWalletAsync(Guid userId, CancellationToken ct);
    Task<UserVoucher?> FindReplayAsync(Guid userId, string key, CancellationToken ct);
    Task<VoucherOffer?> GetOfferAsync(Guid offerId, CancellationToken ct);
    Task<Campaign?> LockCampaignAsync(Guid campaignId, CancellationToken ct);
    Task<int> CountClaimsAsync(Guid userId, Guid campaignId, CancellationToken ct);
    Task<bool> TryIssueAsync(Guid offerId, Guid campaignId, DateTime now, CancellationToken ct);
    void Record(UserVoucher voucher, PointTransaction entry, AuditLog audit);
    Task CommitAsync(CancellationToken ct);
}
public interface IVoucherQueries
{
    Task<Page<UserVoucherDetails>> MineAsync(Guid userId, int page,
        int pageSize, bool? expired, CancellationToken cancellationToken = default);
    Task<UserVoucherDetails?> GetMineAsync(Guid userId, Guid voucherId, CancellationToken cancellationToken = default);
}
