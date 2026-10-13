using VoucherHub.Application.Common.Persistence;
using VoucherHub.Domain.Entities;
namespace VoucherHub.Application.Campaigns.Ports;
public interface ICampaignStore
{
    Task<IWriteTransaction> BeginAsync(CancellationToken ct);
    Task<User?> LockActorAsync(Guid actorId, CancellationToken ct);
    Task<Campaign?> LockCampaignAsync(Guid id, CancellationToken ct);
    Task<Campaign?> GetAsync(Guid id, CancellationToken ct);
    Task<VoucherOffer?> GetOfferAsync(Guid campaignId, Guid offerId, CancellationToken ct);
    Task<List<VoucherOffer>> GetOffersAsync(Guid campaignId, CancellationToken ct);
    Task<bool> HasInvalidExpiryAsync(Guid campaignId, DateTime endsAt, CancellationToken ct);
    void Add(Campaign campaign);
    void Add(VoucherOffer offer);
    void Add(AuditLog audit);
}
