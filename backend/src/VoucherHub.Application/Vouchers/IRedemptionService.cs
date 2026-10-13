using VoucherHub.Application.Common.Pagination;
using VoucherHub.Application.Users;

namespace VoucherHub.Application.Vouchers;

public sealed record UserVoucherDetails(Guid Id, Guid VoucherOfferId, Guid CampaignId,
    string OfferName, string CampaignName, string Code, long PointsSpent,
    DateTime ClaimedAt, DateTime ExpiresAt, bool IsExpired);

public enum RedeemStatus { Created, Replayed, Forbidden, NotFound, Conflict }
public sealed record RedeemResult(RedeemStatus Status, UserVoucherDetails? Voucher = null,
    string? Code = null);

public interface IRedemptionService
{
    Task<RedeemResult> RedeemAsync(Guid userId, Guid offerId, string requestKey,
        CancellationToken cancellationToken = default);
    Task<Page<UserVoucherDetails>> MineAsync(Guid userId, int page, int pageSize,
        bool? expired, CancellationToken cancellationToken = default);
    Task<UserVoucherDetails?> GetMineAsync(Guid userId, Guid voucherId,
        CancellationToken cancellationToken = default);
}
