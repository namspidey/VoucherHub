namespace VoucherHub.Application.Identity;

public sealed record LoginCommand(string Email, string Password);

public enum LoginStatus
{
    Success,
    InvalidCredentials,
    Blocked
}

public sealed record LoginResult(
    LoginStatus Status,
    string? AccessToken = null,
    DateTime? ExpiresAtUtc = null);

public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default);
}
