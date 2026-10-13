using VoucherHub.Application.Common.Pagination;
using Microsoft.EntityFrameworkCore;
using VoucherHub.Application.Campaigns;
using VoucherHub.Application.Users;
using VoucherHub.Domain.Entities;
using VoucherHub.Infrastructure.Persistence;

namespace VoucherHub.Infrastructure.Persistence.Campaigns;

public sealed class CampaignCatalog(VoucherHubDbContext db, TimeProvider clock) : ICampaignCatalog
{
    public async Task<Page<CatalogCampaign>> PublicListAsync(int page, int pageSize,
        string? phase, CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var query = db.Campaigns.AsNoTracking().Where(x => x.Status == "PUBLISHED");
        query = phase switch
        {
            "UPCOMING" => query.Where(x => x.StartsAt > now),
            "ONGOING" => query.Where(x => x.StartsAt <= now && x.EndsAt > now),
            "ENDED" => query.Where(x => x.EndsAt <= now),
            _ => query
        };
        var count = await query.CountAsync(cancellationToken);
        var campaigns = await query.OrderBy(x => x.StartsAt).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var ids = campaigns.Select(x => x.Id).ToArray();
        var offers = await db.VoucherOffers.AsNoTracking().Where(x => ids.Contains(x.CampaignId))
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var lookup = offers.ToLookup(x => x.CampaignId);
        return new(campaigns.Select(x => Map(x, lookup[x.Id], now)).ToList(),
            page, pageSize, count);
    }

    public async Task<CatalogCampaign?> PublicGetAsync(Guid id,
        CancellationToken cancellationToken = default)
    {
        var campaign = await db.Campaigns.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == id && x.Status == "PUBLISHED", cancellationToken);
        if (campaign is null) return null;
        var offers = await db.VoucherOffers.AsNoTracking().Where(x => x.CampaignId == id)
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        return Map(campaign, offers, clock.GetUtcNow().UtcDateTime);
    }

    public async Task<Page<CampaignDetails>> AdminListAsync(int page, int pageSize,
        string? status, string? search, CancellationToken cancellationToken = default)
    {
        var query = db.Campaigns.AsNoTracking().AsQueryable();
        if (status is not null) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{search}%"));
        var count = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new CampaignDetails(x.Id, x.Name, x.Description,
                x.StartsAt, x.EndsAt, x.Status, x.MaxRedemptionsPerUser,
                x.CreatedBy, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);
        return new(rows, page, pageSize, count);
    }

    private static CatalogCampaign Map(Campaign campaign,
        IEnumerable<VoucherOffer> offers, DateTime now) =>
        new(campaign.Id, campaign.Name, campaign.Description,
            campaign.StartsAt, campaign.EndsAt, campaign.Status,
            now < campaign.StartsAt ? "UPCOMING" :
                now < campaign.EndsAt ? "ONGOING" : "ENDED",
            campaign.MaxRedemptionsPerUser,
            offers.Select(x => new CatalogOffer(x.Id, x.Name, x.Description,
                x.PointsCost, x.TotalQuantity, x.IssuedQuantity,
                x.TotalQuantity - x.IssuedQuantity, x.ExpiresAt)).ToList());
}
