namespace VoucherHub.Application.Common.Security;
public interface IUserAccess
{
    Task<bool> IsActiveAdminAsync(Guid userId, CancellationToken cancellationToken = default);
}
