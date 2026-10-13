using VoucherHub.Application.Common.Pagination;
namespace VoucherHub.Application.Wallets;


public sealed record PointEntry(Guid Id, string Type, long Amount, long BalanceAfter,
    Guid? UserVoucherId, Guid PerformedBy, string Reason, DateTime CreatedAt);

public sealed record GrantReceipt(Guid TransactionId, Guid UserId, long Amount,
    long BalanceAfter, Guid PerformedBy, string Reason, DateTime CreatedAt);

public enum GrantStatus { Success, Forbidden, NotFound, Conflict, InvalidInput }
public sealed record GrantResult(GrantStatus Status, GrantReceipt? Receipt = null,
    string? Code = null);

public interface IWalletService
{
    Task<GrantResult> GrantAsync(Guid actorId, Guid userId, long amount,
        string reason, CancellationToken cancellationToken = default);
    Task<Page<PointEntry>?> HistoryAsync(Guid userId, int page, int pageSize,
        string? type, CancellationToken cancellationToken = default);
}
