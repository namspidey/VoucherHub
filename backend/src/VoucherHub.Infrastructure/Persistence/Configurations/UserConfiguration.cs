using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoucherHub.Domain.Entities;

namespace VoucherHub.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        var user = builder;

        user.ToTable("users", table =>
        {
            table.HasCheckConstraint("ck_users_role", "role IN ('ADMIN', 'USER')");
            table.HasCheckConstraint("ck_users_status", "status IN ('ACTIVE', 'BLOCKED')");
        });

        user.HasKey(x => x.Id);

        user.Property(x => x.Id)
            .HasColumnName("id");

        user.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        user.HasIndex(x => x.Email)
            .IsUnique();

        user.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();

        user.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100)
            .IsRequired();

        user.Property(x => x.Role)
            .HasColumnName("role")
            .HasMaxLength(20)
            .IsRequired();

        user.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        user.Property(x => x.EmailVerifiedAt)
            .HasColumnName("email_verified_at");

        user.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        user.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
