using Microsoft.EntityFrameworkCore;
using VoucherHub.Application.Users.Ports;
using VoucherHub.Application.Common.Persistence;
using VoucherHub.Domain.Entities;
namespace VoucherHub.Infrastructure.Persistence.Identity;
public sealed class AdminUserStore(VoucherHubDbContext db) : IAdminUserStore
{
    public async Task<IWriteTransaction> BeginAsync(CancellationToken ct) => new WriteTransaction(db, await db.Database.BeginTransactionAsync(ct));
    public Task<User?> LockUserAsync(Guid id, CancellationToken ct) =>
        db.Users.FromSqlInterpolated($"SELECT * FROM users WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
    public void Add(AuditLog entity) => db.AuditLogs.Add(entity);
}
