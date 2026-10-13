using Microsoft.AspNetCore.Identity;
using VoucherHub.Application.Identity.Ports;
using VoucherHub.Domain.Entities;
namespace VoucherHub.Infrastructure.Identity;
public sealed class UserPasswordService(IPasswordHasher<User> hasher) : IUserPasswordService
{
    public bool Verify(User user, string password, out bool needsRehash)
    {
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        needsRehash = result == PasswordVerificationResult.SuccessRehashNeeded;
        return result != PasswordVerificationResult.Failed;
    }
    public string Hash(User user, string password) => hasher.HashPassword(user, password);
}
