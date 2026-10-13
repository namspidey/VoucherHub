using VoucherHub.Application.Common.Pagination;
using VoucherHub.Application.Users;

namespace VoucherHub.Application.Campaigns;

public sealed record CatalogOffer(Guid Id, string Name, string? Description,
    long PointsCost, int TotalQuantity, int IssuedQuantity, int RemainingQuantity,
    DateTime ExpiresAt);
public sealed record CatalogCampaign(Guid Id, string Name, string? Description,
    DateTime StartsAt, DateTime EndsAt, string Status, string Phase,
    int MaxRedemptionsPerUser, IReadOnlyList<CatalogOffer> Offers);

public interface ICampaignCatalog
{
    Task<Page<CatalogCampaign>> PublicListAsync(int page, int pageSize, string? phase,
        CancellationToken cancellationToken = default);
    Task<CatalogCampaign?> PublicGetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Page<CampaignDetails>> AdminListAsync(int page, int pageSize, string? status,
        string? search, CancellationToken cancellationToken = default);
}
