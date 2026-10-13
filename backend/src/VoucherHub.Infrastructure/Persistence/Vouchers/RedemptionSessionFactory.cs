using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VoucherHub.Application.Vouchers.Ports;
using VoucherHub.Domain.Entities;
namespace VoucherHub.Infrastructure.Persistence.Vouchers;

public sealed class RedemptionSessionFactory(VoucherHubDbContext db) : IRedemptionSessionFactory
{
    public async Task<IRedemptionSession> BeginAsync(CancellationToken ct) =>
        new Session(db, await db.Database.BeginTransactionAsync(ct));

    private sealed class Session(VoucherHubDbContext db, IDbContextTransaction tx) : IRedemptionSession
    {
        public Task<User?> LockUserAsync(Guid id, CancellationToken ct) =>
            db.Users.FromSqlInterpolated($"SELECT * FROM users WHERE id = {id} FOR SHARE").SingleOrDefaultAsync(ct);
        public Task<PointWallet?> LockWalletAsync(Guid id, CancellationToken ct) =>
            db.PointWallets.FromSqlInterpolated($"SELECT * FROM point_wallets WHERE user_id = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
        public Task<UserVoucher?> FindReplayAsync(Guid id, string key, CancellationToken ct) =>
            db.UserVouchers.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == id && x.RequestKey == key, ct);
        public Task<VoucherOffer?> GetOfferAsync(Guid id, CancellationToken ct) =>
            db.VoucherOffers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        public Task<Campaign?> LockCampaignAsync(Guid id, CancellationToken ct) =>
            db.Campaigns.FromSqlInterpolated($"SELECT * FROM campaigns WHERE id = {id} FOR SHARE").AsNoTracking().SingleOrDefaultAsync(ct);
        public Task<int> CountClaimsAsync(Guid userId, Guid campaignId, CancellationToken ct) =>
            (from v in db.UserVouchers.AsNoTracking()
             join o in db.VoucherOffers.AsNoTracking() on v.VoucherOfferId equals o.Id
             where v.UserId == userId && o.CampaignId == campaignId select v.Id).CountAsync(ct);
        public async Task<bool> TryIssueAsync(Guid offerId, Guid campaignId, DateTime now, CancellationToken ct) =>
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE voucher_offers SET issued_quantity = issued_quantity + 1, updated_at = {now}
                WHERE id = {offerId} AND campaign_id = {campaignId} AND issued_quantity < total_quantity
                """, ct) == 1;
        public void Record(UserVoucher voucher, PointTransaction entry, AuditLog audit)
        {
            db.UserVouchers.Add(voucher); db.PointTransactions.Add(entry); db.AuditLogs.Add(audit);
        }
        public async Task CommitAsync(CancellationToken ct)
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        public ValueTask DisposeAsync() => tx.DisposeAsync();
    }
}
