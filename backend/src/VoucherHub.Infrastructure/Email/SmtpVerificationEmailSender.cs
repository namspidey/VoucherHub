using VoucherHub.Application.Identity;
using VoucherHub.Application.Identity.Ports;
using VoucherHub.Infrastructure.Identity;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace VoucherHub.Infrastructure.Email;

public sealed class SmtpVerificationEmailSender : IVerificationEmailSender
{
    private readonly SmtpOptions _smtpOptions;
    private readonly RegistrationOptions _registrationOptions;

    public SmtpVerificationEmailSender(
        IOptions<SmtpOptions> smtpOptions,
        IOptions<RegistrationOptions> registrationOptions)
    {
        _smtpOptions = smtpOptions.Value;
        _registrationOptions = registrationOptions.Value;
    }

    public async Task SendVerificationEmailAsync(
        string email,
        string displayName,
        string token,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigurationIsValid();

        var verificationUrl =
            $"{_registrationOptions.VerificationPageUrl}?token={Uri.EscapeDataString(token)}";

        using var message = new MailMessage
        {
            From = new MailAddress(_smtpOptions.FromEmail, _smtpOptions.FromName),
            Subject = "Verify your VoucherHub account",
            Body = $"""
                <p>Hello {WebUtility.HtmlEncode(displayName)},</p>
                <p>Use the following link to verify your VoucherHub account:</p>
                <p><a href="{WebUtility.HtmlEncode(verificationUrl)}">Verify email</a></p>
                <p>The link expires in {_registrationOptions.VerificationTokenLifetimeMinutes} minutes.</p>
                """,
            IsBodyHtml = true
        };
        message.To.Add(email);

        using var smtpClient = new SmtpClient(_smtpOptions.Host, _smtpOptions.Port)
        {
            EnableSsl = _smtpOptions.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_smtpOptions.Username))
        {
            smtpClient.Credentials = new NetworkCredential(
                _smtpOptions.Username,
                _smtpOptions.Password);
        }

        await smtpClient.SendMailAsync(message, cancellationToken);
    }

    private void EnsureConfigurationIsValid()
    {
        if (string.IsNullOrWhiteSpace(_smtpOptions.Host) ||
            string.IsNullOrWhiteSpace(_smtpOptions.FromEmail) ||
            string.IsNullOrWhiteSpace(_registrationOptions.VerificationPageUrl))
        {
            throw new InvalidOperationException(
                "SMTP and verification page settings are not configured.");
        }
    }
}
