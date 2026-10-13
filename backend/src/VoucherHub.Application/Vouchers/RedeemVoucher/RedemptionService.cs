using VoucherHub.Application.Common.Pagination;
using System.Security.Cryptography;
using System.Text.Json;
using VoucherHub.Application.Vouchers.Ports;
using VoucherHub.Application.Users;
using VoucherHub.Domain.Entities;
using VoucherHub.Domain.Campaigns;

namespace VoucherHub.Application.Vouchers.RedeemVoucher;

public sealed class RedemptionService(IRedemptionSessionFactory sessions, IVoucherQueries queries,
    TimeProvider clock) : IRedemptionService
{
    public async Task<RedeemResult> RedeemAsync(Guid userId, Guid offerId, string requestKey,
        CancellationToken cancellationToken = default)
    {
        await using var session = await sessions.BeginAsync(cancellationToken);
        var user = await session.LockUserAsync(userId, cancellationToken);
        if (user is null || user.Role != "USER" || user.Status != "ACTIVE")
            return new(RedeemStatus.Forbidden, Code: "USER_NOT_ACTIVE");
        var wallet = await session.LockWalletAsync(userId, cancellationToken);
        if (wallet is null) return new(RedeemStatus.Conflict, Code: "WALLET_MISSING");
        var prior = await session.FindReplayAsync(userId, requestKey, cancellationToken);
        if (prior is not null)
        {
            if (prior.VoucherOfferId != offerId)
                return new(RedeemStatus.Conflict, Code: "IDEMPOTENCY_KEY_REUSED");
            return new(RedeemStatus.Replayed, await queries.GetMineAsync(userId, prior.Id, cancellationToken));
        }
        var offer = await session.GetOfferAsync(offerId, cancellationToken);
        if (offer is null) return new(RedeemStatus.NotFound, Code: "OFFER_NOT_FOUND");
        var campaign = await session.LockCampaignAsync(offer.CampaignId, cancellationToken);
        if (campaign is null) return new(RedeemStatus.NotFound, Code: "CAMPAIGN_NOT_FOUND");
        // Read the offer again under the campaign lock to coordinate draft edits/publish.
        offer = await session.GetOfferAsync(offerId, cancellationToken);
        if (offer is null || offer.CampaignId != campaign.Id)
            return new(RedeemStatus.NotFound, Code: "OFFER_NOT_FOUND");
        var now = clock.GetUtcNow().UtcDateTime;
        var unavailable = CampaignRules.RedemptionError(campaign, now);
        if (unavailable is not null) return new(RedeemStatus.Conflict, Code: unavailable);
        var used = await session.CountClaimsAsync(userId, campaign.Id, cancellationToken);
        if (used >= campaign.MaxRedemptionsPerUser)
            return new(RedeemStatus.Conflict, Code: "CAMPAIGN_QUOTA_EXCEEDED");
        if (wallet.Balance < offer.PointsCost)
            return new(RedeemStatus.Conflict, Code: "INSUFFICIENT_POINTS");
        if (!await session.TryIssueAsync(offerId, campaign.Id, now, cancellationToken))
            return new(RedeemStatus.Conflict, Code: "OUT_OF_STOCK");
        wallet.Balance -= offer.PointsCost;
        wallet.UpdatedAt = now;
        var voucher = new UserVoucher
        {
            Id = Guid.NewGuid(), UserId = userId, VoucherOfferId = offerId,
            Code = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)),
            RequestKey = requestKey, PointsSpent = offer.PointsCost,
            ClaimedAt = now, ExpiresAt = offer.ExpiresAt
        };
        session.Record(voucher, new PointTransaction
        {
            Id = Guid.NewGuid(), WalletId = wallet.Id, Type = "VOUCHER_REDEMPTION",
            Amount = -offer.PointsCost, BalanceAfter = wallet.Balance, UserVoucherId = voucher.Id,
            PerformedBy = userId, Reason = "Voucher redemption", CreatedAt = now
        }, new AuditLog
        {
            Id = Guid.NewGuid(), ActorUserId = userId, Action = "VOUCHER_REDEEMED",
            EntityType = "UserVoucher", EntityId = voucher.Id, CreatedAt = now,
            Details = JsonSerializer.Serialize(new { offerId, campaignId = campaign.Id, pointsSpent = offer.PointsCost })
        });
        await session.CommitAsync(cancellationToken);
        return new(RedeemStatus.Created, new(voucher.Id, offerId, campaign.Id, offer.Name,
            campaign.Name, voucher.Code, voucher.PointsSpent, now, offer.ExpiresAt, false));
    }
    public Task<Page<UserVoucherDetails>> MineAsync(Guid userId, int page, int pageSize,
        bool? expired, CancellationToken cancellationToken = default) =>
        queries.MineAsync(userId, page, pageSize, expired, cancellationToken);
    public Task<UserVoucherDetails?> GetMineAsync(Guid userId, Guid id,
        CancellationToken cancellationToken = default) => queries.GetMineAsync(userId, id, cancellationToken);
}
