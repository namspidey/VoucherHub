using VoucherHub.Application.Wallets;
using VoucherHub.Application.Common.Pagination;
using VoucherHub.Domain.Entities;
using VoucherHub.Application.Users;
namespace VoucherHub.Application.Wallets.Ports;
public interface IWalletSessionFactory { Task<IWalletSession> BeginAsync(CancellationToken ct); }
public interface IWalletSession : IAsyncDisposable
{
    Task<User?> LockUserAsync(Guid id, CancellationToken ct);
    Task<PointWallet?> LockWalletAsync(Guid userId, CancellationToken ct);
    void Record(PointTransaction entry, AuditLog audit);
    Task CommitAsync(CancellationToken ct);
}
public interface IPointHistoryQueries
{
    Task<Page<PointEntry>?> HistoryAsync(Guid userId, int page, int pageSize, string? type, CancellationToken cancellationToken = default);
}
