using VoucherHub.Application.Common.Pagination;
namespace VoucherHub.Application.Users;

public sealed record AdminUserSummary(Guid Id, string Email, string DisplayName,
    string Role, string Status, long Balance, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record BlockReceipt(Guid Id, string Status, DateTime UpdatedAt);
public enum BlockStatus { Success, Forbidden, NotFound, InvalidInput, Conflict }
public sealed record BlockResult(BlockStatus Status, BlockReceipt? Receipt = null,
    string? Code = null);
public sealed record IssuedVoucherEntry(Guid Id, Guid UserId, Guid VoucherOfferId,
    Guid CampaignId, long PointsSpent, DateTime ClaimedAt, DateTime ExpiresAt);
public sealed record AuditEntry(Guid Id, Guid? ActorUserId, string Action,
    string EntityType, Guid EntityId, string? Details, DateTime CreatedAt);

public interface IAdminUsersService
{
    Task<Page<AdminUserSummary>> ListUsersAsync(int page, int pageSize,
        string? email, string? status, CancellationToken cancellationToken = default);
    Task<BlockResult> BlockAsync(Guid actorId, Guid userId, string reason,
        CancellationToken cancellationToken = default);
    Task<Page<IssuedVoucherEntry>> IssuedAsync(int page, int pageSize,
        Guid? userId, Guid? campaignId, Guid? offerId, DateTime? from, DateTime? to,
        CancellationToken cancellationToken = default);
    Task<Page<AuditEntry>> AuditAsync(int page, int pageSize,
        Guid? actorId, string? action, string? entityType, Guid? entityId,
        DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}
