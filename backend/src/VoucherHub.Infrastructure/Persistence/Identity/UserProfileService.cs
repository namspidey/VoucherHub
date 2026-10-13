using Microsoft.EntityFrameworkCore;
using VoucherHub.Application.Users;
using VoucherHub.Infrastructure.Persistence;

namespace VoucherHub.Infrastructure.Persistence.Identity;

public sealed class UserProfileService : IUserProfileService
{
    private readonly VoucherHubDbContext _dbContext;

    public UserProfileService(VoucherHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserProfile?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        (from user in _dbContext.Users.AsNoTracking()
         join wallet in _dbContext.PointWallets.AsNoTracking()
             on user.Id equals wallet.UserId
         where user.Id == userId
         select new UserProfile(
             user.Id,
             user.Email,
             user.DisplayName,
             user.Role,
             user.Status,
             user.EmailVerifiedAt,
             wallet.Balance))
        .SingleOrDefaultAsync(cancellationToken);
}
