namespace VoucherHub.Application.Identity.Ports;

public interface IVerificationEmailSender
{
    Task SendVerificationEmailAsync(
        string email,
        string displayName,
        string token,
        CancellationToken cancellationToken = default);
}
