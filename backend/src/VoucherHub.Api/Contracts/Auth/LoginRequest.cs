using System.ComponentModel.DataAnnotations;

namespace VoucherHub.Api.Contracts.Auth;

public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; init; } = string.Empty;

    [Required, MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}