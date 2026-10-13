namespace VoucherHub.Domain.Entities;

public class PendingRegistration
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string VerificationTokenHash { get; set; } = string.Empty;
    public DateTime TokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastSentAt { get; set; }
}
