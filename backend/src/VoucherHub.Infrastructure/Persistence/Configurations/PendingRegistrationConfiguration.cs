using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoucherHub.Domain.Entities;

namespace VoucherHub.Infrastructure.Persistence.Configurations;

public sealed class PendingRegistrationConfiguration : IEntityTypeConfiguration<PendingRegistration>
{
    public void Configure(EntityTypeBuilder<PendingRegistration> builder)
    {
        var pendingRegistration = builder;

        pendingRegistration.ToTable("pending_registrations");
        pendingRegistration.HasKey(x => x.Id);

        pendingRegistration.Property(x => x.Id)
            .HasColumnName("id");

        pendingRegistration.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        pendingRegistration.HasIndex(x => x.Email)
            .IsUnique();

        pendingRegistration.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();

        pendingRegistration.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100)
            .IsRequired();

        pendingRegistration.Property(x => x.VerificationTokenHash)
            .HasColumnName("verification_token_hash")
            .HasMaxLength(128)
            .IsRequired();

        pendingRegistration.HasIndex(x => x.VerificationTokenHash)
            .IsUnique();

        pendingRegistration.Property(x => x.TokenExpiresAt)
            .HasColumnName("token_expires_at");

        pendingRegistration.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        pendingRegistration.Property(x => x.LastSentAt)
            .HasColumnName("last_sent_at");
    }
}
