namespace VoucherHub.Application.Identity;

public sealed class RegistrationOptions
{
    public const string SectionName = "Registration";

    public int VerificationTokenLifetimeMinutes { get; set; } = 15;
    public int PendingRegistrationRetentionHours { get; set; } = 24;
    public int ResendCooldownSeconds { get; set; } = 60;
    public string VerificationPageUrl { get; set; } = string.Empty;
}
