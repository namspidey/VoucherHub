using System.ComponentModel.DataAnnotations;

namespace VoucherHub.Api.Contracts.Auth;

public sealed class ResendVerificationRequest
{
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; init; } = string.Empty;
}