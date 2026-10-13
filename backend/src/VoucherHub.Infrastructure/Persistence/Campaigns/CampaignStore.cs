using Microsoft.EntityFrameworkCore;
using VoucherHub.Application.Campaigns.Ports;
using VoucherHub.Application.Common.Persistence;
using VoucherHub.Domain.Entities;
namespace VoucherHub.Infrastructure.Persistence.Campaigns;
public sealed class CampaignStore(VoucherHubDbContext db) : ICampaignStore
{
    public async Task<IWriteTransaction> BeginAsync(CancellationToken ct) => new WriteTransaction(db, await db.Database.BeginTransactionAsync(ct));
    public Task<User?> LockActorAsync(Guid id, CancellationToken ct) =>
        db.Users.FromSqlInterpolated($"SELECT * FROM users WHERE id = {id} FOR SHARE").SingleOrDefaultAsync(ct);
    public Task<Campaign?> LockCampaignAsync(Guid id, CancellationToken ct) =>
        db.Campaigns.FromSqlInterpolated($"SELECT * FROM campaigns WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
    public Task<Campaign?> GetAsync(Guid id, CancellationToken ct) => db.Campaigns.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<VoucherOffer?> GetOfferAsync(Guid campaignId, Guid offerId, CancellationToken ct) =>
        db.VoucherOffers.SingleOrDefaultAsync(x => x.Id == offerId && x.CampaignId == campaignId, ct);
    public Task<List<VoucherOffer>> GetOffersAsync(Guid id, CancellationToken ct) =>
        db.VoucherOffers.Where(x => x.CampaignId == id).OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(ct);
    public Task<bool> HasInvalidExpiryAsync(Guid id, DateTime endsAt, CancellationToken ct) =>
        db.VoucherOffers.AnyAsync(x => x.CampaignId == id && x.ExpiresAt <= endsAt, ct);
    public void Add(Campaign entity) => db.Campaigns.Add(entity);
    public void Add(VoucherOffer entity) => db.VoucherOffers.Add(entity);
    public void Add(AuditLog entity) => db.AuditLogs.Add(entity);
}
