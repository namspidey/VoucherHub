using VoucherHub.Application.Identity.Ports;
namespace VoucherHub.Application.Identity.Login;
public sealed class AuthenticationService(ILoginStore store, IUserPasswordService passwords,
    IAccessTokenIssuer tokens, TimeProvider clock) : IAuthenticationService
{
    public async Task<LoginResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var user = await store.FindByEmailAsync(command.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !passwords.Verify(user, command.Password, out var rehash))
            return new(LoginStatus.InvalidCredentials);
        if (user.Status == "BLOCKED") return new(LoginStatus.Blocked);
        var now = clock.GetUtcNow().UtcDateTime;
        if (rehash)
        {
            user.PasswordHash = passwords.Hash(user, command.Password); user.UpdatedAt = now;
            await store.SaveAsync(cancellationToken);
        }
        return tokens.Issue(user, now);
    }
}
