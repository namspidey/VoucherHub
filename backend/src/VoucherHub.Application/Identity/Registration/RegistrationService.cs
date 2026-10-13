using VoucherHub.Application.Identity.Ports;
using VoucherHub.Application.Common.Persistence;
using System.Security.Cryptography;
using VoucherHub.Application.Identity;
using VoucherHub.Domain.Entities;

namespace VoucherHub.Application.Identity.Registration;

public sealed class RegistrationService : IRegistrationService
{
    private readonly IRegistrationStore _store;
    private readonly IPendingPasswordService _passwordHasher;
    private readonly IVerificationEmailSender _emailSender;
    private readonly RegistrationOptions _options;
    private readonly TimeProvider _timeProvider;

    public RegistrationService(
        IRegistrationStore store,
        IPendingPasswordService passwordHasher,
        IVerificationEmailSender emailSender,
        RegistrationOptions options,
        TimeProvider timeProvider)
    {
        _store = store;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _options = options;
        _timeProvider = timeProvider;
    }

    public async Task RegisterAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = GetUtcNow();
        var normalizedEmail = NormalizeEmail(command.Email);

        await DeleteExpiredPendingRegistrationsAsync(now, cancellationToken);

        var token = GenerateToken();
        string? emailToSend = null;
        string? displayNameToSend = null;

        await using var transaction = await _store.BeginAsync(cancellationToken);

        try
        {
            await _store.LockEmailAsync(normalizedEmail, cancellationToken);

            if (await _store.EmailExistsAsync(normalizedEmail, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            var pendingRegistration = await FindPendingByEmailForUpdateAsync(
                normalizedEmail,
                cancellationToken);

            if (pendingRegistration is not null &&
                pendingRegistration.LastSentAt.AddSeconds(
                    _options.ResendCooldownSeconds) > now)
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            if (pendingRegistration is null)
            {
                pendingRegistration = new PendingRegistration
                {
                    Id = Guid.NewGuid(),
                    Email = normalizedEmail,
                    CreatedAt = now
                };
                _store.Add(pendingRegistration);
            }
            else if (pendingRegistration.CreatedAt.AddHours(
                _options.PendingRegistrationRetentionHours) <= now)
            {
                // A new registration cycle must not be removed as a stale pending row.
                pendingRegistration.CreatedAt = now;
            }

            pendingRegistration.DisplayName = command.DisplayName.Trim();
            pendingRegistration.PasswordHash = _passwordHasher.Hash(
                pendingRegistration,
                command.Password);
            pendingRegistration.VerificationTokenHash = HashToken(token);
            pendingRegistration.TokenExpiresAt = now.AddMinutes(
                _options.VerificationTokenLifetimeMinutes);
            pendingRegistration.LastSentAt = now;

            await transaction.CommitAsync(cancellationToken);

            emailToSend = pendingRegistration.Email;
            displayNameToSend = pendingRegistration.DisplayName;
        }
        catch (StorageConflictException exception)
            when (IsUniqueViolation(exception, "IX_pending_registrations_email"))
        {
            await transaction.RollbackAsync(cancellationToken);
            return;
        }

        await _emailSender.SendVerificationEmailAsync(
            emailToSend,
            displayNameToSend,
            token,
            cancellationToken);
    }

    public async Task ResendVerificationAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var now = GetUtcNow();
        var normalizedEmail = NormalizeEmail(email);
        var token = GenerateToken();
        string? emailToSend = null;
        string? displayNameToSend = null;

        await using var transaction = await _store.BeginAsync(cancellationToken);

        var pendingRegistration = await FindPendingByEmailForUpdateAsync(
            normalizedEmail,
            cancellationToken);

        if (pendingRegistration is null ||
            pendingRegistration.CreatedAt.AddHours(
                _options.PendingRegistrationRetentionHours) <= now ||
            pendingRegistration.LastSentAt.AddSeconds(
                _options.ResendCooldownSeconds) > now)
        {
            await transaction.RollbackAsync(cancellationToken);
            return;
        }

        pendingRegistration.VerificationTokenHash = HashToken(token);
        pendingRegistration.TokenExpiresAt = now.AddMinutes(
            _options.VerificationTokenLifetimeMinutes);
        pendingRegistration.LastSentAt = now;

        await transaction.CommitAsync(cancellationToken);

        emailToSend = pendingRegistration.Email;
        displayNameToSend = pendingRegistration.DisplayName;

        await _emailSender.SendVerificationEmailAsync(
            emailToSend,
            displayNameToSend,
            token,
            cancellationToken);
    }

    public async Task<VerifyEmailResult> VerifyEmailAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var now = GetUtcNow();
        var tokenHash = HashToken(token);

        await using var transaction = await _store.BeginAsync(cancellationToken);

        var pendingRegistration = await _store.LockPendingTokenAsync(tokenHash, cancellationToken);

        if (pendingRegistration is null ||
            pendingRegistration.TokenExpiresAt <= now)
        {
            await transaction.RollbackAsync(cancellationToken);
            return VerifyEmailResult.InvalidOrExpired;
        }

        if (await _store.EmailExistsAsync(pendingRegistration.Email, cancellationToken))
        {
            _store.Remove(pendingRegistration);

            await transaction.CommitAsync(cancellationToken);
            return VerifyEmailResult.EmailAlreadyRegistered;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = pendingRegistration.Email,
            PasswordHash = pendingRegistration.PasswordHash,
            DisplayName = pendingRegistration.DisplayName,
            Role = "USER",
            Status = "ACTIVE",
            EmailVerifiedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        var wallet = new PointWallet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Balance = 0L,
            UpdatedAt = now
        };

        _store.Add(user);
        _store.Add(wallet);
        _store.Remove(pendingRegistration);

        try
        {

            await transaction.CommitAsync(cancellationToken);
            return VerifyEmailResult.Success;
        }
        catch (StorageConflictException exception)
            when (IsUniqueViolation(exception, "IX_users_email"))
        {
            await transaction.RollbackAsync(cancellationToken);
            return VerifyEmailResult.EmailAlreadyRegistered;
        }
    }

    private Task<PendingRegistration?> FindPendingByEmailForUpdateAsync(string email, CancellationToken ct) =>
        _store.LockPendingEmailAsync(email, ct);
    private Task DeleteExpiredPendingRegistrationsAsync(DateTime now, CancellationToken ct) =>
        _store.DeleteExpiredAsync(now.AddHours(-_options.PendingRegistrationRetentionHours), ct);

    private DateTime GetUtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string HashToken(string token)
    {
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(token);
        return Convert.ToHexString(SHA256.HashData(tokenBytes));
    }

    private static bool IsUniqueViolation(StorageConflictException exception, string name) =>
        exception.ConstraintName == name;
}
