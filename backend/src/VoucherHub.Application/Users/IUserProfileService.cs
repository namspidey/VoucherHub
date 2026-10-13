namespace VoucherHub.Application.Users;

public sealed record UserProfile(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    string Status,
    DateTime EmailVerifiedAt,
    long Balance);

public interface IUserProfileService
{
    Task<UserProfile?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}