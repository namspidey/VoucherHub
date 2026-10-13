namespace VoucherHub.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "VoucherHub";
    public string Audience { get; set; } = "VoucherHub.Client";
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public string SigningKey { get; set; } = string.Empty;
}
