using Microsoft.EntityFrameworkCore;
using VoucherHub.Application.Common.Security;
namespace VoucherHub.Infrastructure.Persistence.Identity;
public sealed class UserAccess(VoucherHubDbContext db) : IUserAccess
{
    public Task<bool> IsActiveAdminAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Users.AsNoTracking().AnyAsync(x => x.Id == userId && x.Role == "ADMIN" && x.Status == "ACTIVE", cancellationToken);
}
