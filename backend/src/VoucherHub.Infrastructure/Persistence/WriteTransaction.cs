using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using VoucherHub.Application.Common.Persistence;
namespace VoucherHub.Infrastructure.Persistence;
internal sealed class WriteTransaction(VoucherHubDbContext db, IDbContextTransaction tx) : IWriteTransaction
{
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        try { await db.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        { throw new StorageConflictException(pg.ConstraintName ?? "", ex); }
    }
    public Task RollbackAsync(CancellationToken cancellationToken) => tx.RollbackAsync(cancellationToken);
    public ValueTask DisposeAsync() => tx.DisposeAsync();
}
