using VoucherHub.Application.Wallets;
using VoucherHub.Application.Common.Pagination;
using Microsoft.EntityFrameworkCore;
using VoucherHub.Application.Users;
using VoucherHub.Application.Wallets.Ports;
namespace VoucherHub.Infrastructure.Persistence.Wallets;
public sealed class PointHistoryQueries(VoucherHubDbContext db) : IPointHistoryQueries
{
    public async Task<Page<PointEntry>?> HistoryAsync(Guid userId, int page, int pageSize,
        string? type, CancellationToken cancellationToken = default)
    {
        var walletId = await db.PointWallets.AsNoTracking()
            .Where(x => x.UserId == userId).Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (walletId is null) return null;
        var query = db.PointTransactions.AsNoTracking().Where(x => x.WalletId == walletId);
        if (type is not null) query = query.Where(x => x.Type == type);
        var count = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new PointEntry(x.Id, x.Type, x.Amount, x.BalanceAfter,
                x.UserVoucherId, x.PerformedBy, x.Reason, x.CreatedAt))
            .ToListAsync(cancellationToken);
        return new(rows, page, pageSize, count);
    }
}
