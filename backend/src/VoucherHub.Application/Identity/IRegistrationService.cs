namespace VoucherHub.Application.Identity;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string DisplayName);

public enum VerifyEmailResult
{
    Success,
    InvalidOrExpired,
    EmailAlreadyRegistered
}

public interface IRegistrationService
{
    Task RegisterAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default);

    Task ResendVerificationAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<VerifyEmailResult> VerifyEmailAsync(
        string token,
        CancellationToken cancellationToken = default);
}
