using VoucherHub.Application.Common.Pagination;
using System.Text.Json;
using VoucherHub.Domain.Entities;
using VoucherHub.Application.Users.Ports;
namespace VoucherHub.Application.Users.BlockUser;
public sealed class AdminUsersService(IAdminUserStore store, IAdminUserQueries queries, TimeProvider clock) : IAdminUsersService
{
    public async Task<BlockResult> BlockAsync(Guid actorId, Guid userId, string reason,
        CancellationToken cancellationToken = default)
    {
        reason = reason?.Trim() ?? string.Empty;
        if (reason.Length is < 1 or > 500)
            return new(BlockStatus.InvalidInput, Code: "INVALID_REASON");
        await using var tx = await store.BeginAsync(cancellationToken);
        User? actor = null;
        User? target = null;
        foreach (var id in new[] { actorId, userId }.Distinct().OrderBy(x => x))
        {
            var user = await store.LockUserAsync(id, cancellationToken);
            if (id == actorId) actor = user;
            if (id == userId) target = user;
        }
        if (actor is null || actor.Role != "ADMIN" || actor.Status != "ACTIVE")
            return new(BlockStatus.Forbidden);
        if (target is null) return new(BlockStatus.NotFound, Code: "USER_NOT_FOUND");
        if (target.Role != "USER")
            return new(BlockStatus.Conflict, Code: "CANNOT_BLOCK_ADMIN");
        if (target.Status == "BLOCKED")
            return new(BlockStatus.Success, new(target.Id, target.Status, target.UpdatedAt));

        var now = clock.GetUtcNow().UtcDateTime;
        target.Status = "BLOCKED";
        target.UpdatedAt = now;
        store.Add(new AuditLog
        {
            Id = Guid.NewGuid(), ActorUserId = actorId, Action = "USER_BLOCKED",
            EntityType = "User", EntityId = userId,
            Details = JsonSerializer.Serialize(new { reason, before = "ACTIVE", after = "BLOCKED" }),
            CreatedAt = now
        });
        await tx.CommitAsync(cancellationToken);
        return new(BlockStatus.Success, new(target.Id, target.Status, target.UpdatedAt));
    }


    public Task<Page<AdminUserSummary>> ListUsersAsync(int page, int size, string? email, string? status, CancellationToken cancellationToken = default) =>
        queries.ListUsersAsync(page, size, email, status, cancellationToken);
    public Task<Page<IssuedVoucherEntry>> IssuedAsync(int page, int size, Guid? userId, Guid? campaignId, Guid? offerId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default) =>
        queries.IssuedAsync(page, size, userId, campaignId, offerId, from, to, cancellationToken);
    public Task<Page<AuditEntry>> AuditAsync(int page, int size, Guid? actorId, string? action, string? entityType, Guid? entityId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default) =>
        queries.AuditAsync(page, size, actorId, action, entityType, entityId, from, to, cancellationToken);
}
