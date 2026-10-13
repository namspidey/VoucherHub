using VoucherHub.Application.Common.Pagination;
using Microsoft.EntityFrameworkCore;
using VoucherHub.Application.Users;
using VoucherHub.Application.Vouchers;
using VoucherHub.Application.Vouchers.Ports;
namespace VoucherHub.Infrastructure.Persistence.Vouchers;

public sealed class VoucherQueries(VoucherHubDbContext db, TimeProvider clock) : IVoucherQueries
{
    public async Task<Page<UserVoucherDetails>> MineAsync(Guid userId, int page,
        int pageSize, bool? expired, CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var query = db.UserVouchers.AsNoTracking().Where(x => x.UserId == userId);
        if (expired.HasValue)
            query = expired.Value ? query.Where(x => x.ExpiresAt <= now)
                : query.Where(x => x.ExpiresAt > now);
        var count = await query.CountAsync(cancellationToken);
        var rows = await (from voucher in query.OrderByDescending(x => x.ClaimedAt)
                .ThenByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
            join offer in db.VoucherOffers.AsNoTracking() on voucher.VoucherOfferId equals offer.Id
            join campaign in db.Campaigns.AsNoTracking() on offer.CampaignId equals campaign.Id
            select new UserVoucherDetails(voucher.Id, offer.Id, campaign.Id, offer.Name,
                campaign.Name, voucher.Code, voucher.PointsSpent, voucher.ClaimedAt,
                voucher.ExpiresAt, voucher.ExpiresAt <= now)).ToListAsync(cancellationToken);
        return new(rows, page, pageSize, count);
    }

    public Task<UserVoucherDetails?> GetMineAsync(Guid userId, Guid voucherId,
        CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return (from voucher in db.UserVouchers.AsNoTracking()
            join offer in db.VoucherOffers.AsNoTracking() on voucher.VoucherOfferId equals offer.Id
            join campaign in db.Campaigns.AsNoTracking() on offer.CampaignId equals campaign.Id
            where voucher.Id == voucherId && voucher.UserId == userId
            select new UserVoucherDetails(voucher.Id, offer.Id, campaign.Id, offer.Name,
                campaign.Name, voucher.Code, voucher.PointsSpent, voucher.ClaimedAt,
                voucher.ExpiresAt, voucher.ExpiresAt <= now)).SingleOrDefaultAsync(cancellationToken);
    }
}
