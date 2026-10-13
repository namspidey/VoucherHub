using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VoucherHub.Application.Wallets.Ports;
using VoucherHub.Domain.Entities;
namespace VoucherHub.Infrastructure.Persistence.Wallets;
public sealed class WalletSessionFactory(VoucherHubDbContext db) : IWalletSessionFactory
{
    public async Task<IWalletSession> BeginAsync(CancellationToken ct) =>
        new Session(db, await db.Database.BeginTransactionAsync(ct));
    private sealed class Session(VoucherHubDbContext db, IDbContextTransaction tx) : IWalletSession
    {
        public Task<User?> LockUserAsync(Guid id, CancellationToken ct) =>
            db.Users.FromSqlInterpolated($"SELECT * FROM users WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
        public Task<PointWallet?> LockWalletAsync(Guid id, CancellationToken ct) =>
            db.PointWallets.FromSqlInterpolated($"SELECT * FROM point_wallets WHERE user_id = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
        public void Record(PointTransaction entry, AuditLog audit)
        {
            db.PointTransactions.Add(entry); db.AuditLogs.Add(audit);
        }
        public async Task CommitAsync(CancellationToken ct)
        {
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
        public ValueTask DisposeAsync() => tx.DisposeAsync();
    }
}
