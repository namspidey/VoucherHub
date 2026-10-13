using VoucherHub.Domain.Entities;
namespace VoucherHub.Application.Identity.Ports;
public interface ILoginStore
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}
public interface IUserPasswordService
{
    bool Verify(User user, string password, out bool needsRehash);
    string Hash(User user, string password);
}
public interface IAccessTokenIssuer { LoginResult Issue(User user, DateTime now); }
