using System.ComponentModel.DataAnnotations;

namespace VoucherHub.Api.Contracts.Auth;

public sealed class VerifyEmailRequest
{
    [Required, MaxLength(256)]
    public string Token { get; init; } = string.Empty;
}
