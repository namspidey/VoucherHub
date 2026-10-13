using VoucherHub.Application.Common.Pagination;
using VoucherHub.Domain.Entities;
using VoucherHub.Application.Common.Persistence;
namespace VoucherHub.Application.Users.Ports;
public interface IAdminUserStore
{
    Task<IWriteTransaction> BeginAsync(CancellationToken ct);
    Task<User?> LockUserAsync(Guid id, CancellationToken ct);
    void Add(AuditLog audit);
}
public interface IAdminUserQueries
{
    Task<Page<AdminUserSummary>> ListUsersAsync(int page, int size, string? email, string? status, CancellationToken cancellationToken);
    Task<Page<IssuedVoucherEntry>> IssuedAsync(int page, int size, Guid? userId, Guid? campaignId, Guid? offerId, DateTime? from, DateTime? to, CancellationToken cancellationToken);
    Task<Page<AuditEntry>> AuditAsync(int page, int size, Guid? actorId, string? action, string? entityType, Guid? entityId, DateTime? from, DateTime? to, CancellationToken cancellationToken);
}
