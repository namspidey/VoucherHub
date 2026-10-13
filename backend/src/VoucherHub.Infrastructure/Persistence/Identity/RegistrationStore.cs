using Microsoft.EntityFrameworkCore;
using VoucherHub.Application.Common.Persistence;
using VoucherHub.Application.Identity.Ports;
using VoucherHub.Domain.Entities;
namespace VoucherHub.Infrastructure.Persistence.Identity;
public sealed class RegistrationStore(VoucherHubDbContext db) : IRegistrationStore
{
    public async Task<IWriteTransaction> BeginAsync(CancellationToken ct) => new WriteTransaction(db, await db.Database.BeginTransactionAsync(ct));
    public async Task LockEmailAsync(string email, CancellationToken ct) =>
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({email}, 0))", ct);
    public Task<bool> EmailExistsAsync(string email, CancellationToken ct) => db.Users.AnyAsync(x => x.Email == email, ct);
    public Task<PendingRegistration?> LockPendingEmailAsync(string email, CancellationToken ct) =>
        db.PendingRegistrations.FromSqlInterpolated($"SELECT * FROM pending_registrations WHERE email = {email} FOR UPDATE").SingleOrDefaultAsync(ct);
    public Task<PendingRegistration?> LockPendingTokenAsync(string hash, CancellationToken ct) =>
        db.PendingRegistrations.FromSqlInterpolated($"SELECT * FROM pending_registrations WHERE verification_token_hash = {hash} FOR UPDATE").SingleOrDefaultAsync(ct);
    public async Task DeleteExpiredAsync(DateTime cutoff, CancellationToken ct) => await db.PendingRegistrations.Where(x => x.CreatedAt < cutoff).ExecuteDeleteAsync(ct);
    public void Add(PendingRegistration entity) => db.PendingRegistrations.Add(entity);
    public void Add(User entity) => db.Users.Add(entity);
    public void Add(PointWallet entity) => db.PointWallets.Add(entity);
    public void Remove(PendingRegistration entity) => db.PendingRegistrations.Remove(entity);
}
