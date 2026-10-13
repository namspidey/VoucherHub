using System.ComponentModel.DataAnnotations;

namespace VoucherHub.Api.Contracts.Auth;

public sealed class RegisterRequest
{
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string DisplayName { get; init; } = string.Empty;
}