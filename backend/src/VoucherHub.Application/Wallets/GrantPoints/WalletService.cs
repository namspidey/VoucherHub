using VoucherHub.Application.Wallets;
using VoucherHub.Application.Common.Pagination;
using System.Text.Json;
using VoucherHub.Application.Users;
using VoucherHub.Application.Wallets.Ports;
using VoucherHub.Domain.Entities;
namespace VoucherHub.Application.Wallets.GrantPoints;
public sealed class WalletService(IWalletSessionFactory sessions, IPointHistoryQueries queries, TimeProvider clock) : IWalletService
{
    public async Task<GrantResult> GrantAsync(Guid actorId, Guid userId, long amount,
        string reason, CancellationToken cancellationToken = default)
    {
        reason = reason?.Trim() ?? string.Empty;
        if (amount <= 0 || reason.Length is < 1 or > 500)
            return new(GrantStatus.InvalidInput, Code: "INVALID_GRANT");

        await using var session = await sessions.BeginAsync(cancellationToken);
        // Lock every user in the same order; the target needs the stronger lock.
        User? actor = null;
        User? target = null;
        foreach (var id in new[] { actorId, userId }.Distinct().OrderBy(x => x))
        {
            var user = await session.LockUserAsync(id, cancellationToken);
            if (id == actorId) actor = user;
            if (id == userId) target = user;
        }
        if (actor is null || actor.Role != "ADMIN" || actor.Status != "ACTIVE")
            return new(GrantStatus.Forbidden);
        if (target is null) return new(GrantStatus.NotFound, Code: "USER_NOT_FOUND");
        if (target.Role != "USER" || target.Status != "ACTIVE")
            return new(GrantStatus.Conflict, Code: "USER_NOT_ACTIVE");

        var wallet = await session.LockWalletAsync(userId, cancellationToken);
        if (wallet is null) return new(GrantStatus.Conflict, Code: "WALLET_MISSING");
        if (wallet.Balance > long.MaxValue - amount)
            return new(GrantStatus.Conflict, Code: "BALANCE_OVERFLOW");

        var now = clock.GetUtcNow().UtcDateTime;
        wallet.Balance += amount;
        wallet.UpdatedAt = now;
        var entry = new PointTransaction
        {
            Id = Guid.NewGuid(), WalletId = wallet.Id, Type = "ADMIN_GRANT",
            Amount = amount, BalanceAfter = wallet.Balance, PerformedBy = actorId,
            Reason = reason, CreatedAt = now
        };
        
        session.Record(entry, new AuditLog
        {
            Id = Guid.NewGuid(), ActorUserId = actorId, Action = "POINTS_GRANTED",
            EntityType = "User", EntityId = userId,
            Details = JsonSerializer.Serialize(new { amount, reason, balanceAfter = wallet.Balance }),
            CreatedAt = now
        });
        await session.CommitAsync(cancellationToken);
        return new(GrantStatus.Success,
            new(entry.Id, userId, amount, wallet.Balance, actorId, reason, now));
    }


    public Task<Page<PointEntry>?> HistoryAsync(Guid userId, int page, int pageSize, string? type,
        CancellationToken cancellationToken = default) => queries.HistoryAsync(userId, page, pageSize, type, cancellationToken);
}
