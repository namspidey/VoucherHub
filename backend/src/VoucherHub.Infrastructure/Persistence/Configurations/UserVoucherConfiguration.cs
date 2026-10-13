using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoucherHub.Domain.Entities;

namespace VoucherHub.Infrastructure.Persistence.Configurations;

public sealed class UserVoucherConfiguration : IEntityTypeConfiguration<UserVoucher>
{
    public void Configure(EntityTypeBuilder<UserVoucher> builder)
    {
        var userVoucher = builder;

        userVoucher.ToTable("user_vouchers", table =>
        {
            table.HasCheckConstraint(
                "ck_user_vouchers_points_spent_positive",
                "points_spent > 0");
        });

        userVoucher.HasKey(x => x.Id);

        userVoucher.Property(x => x.Id)
            .HasColumnName("id");

        userVoucher.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        userVoucher.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        userVoucher.Property(x => x.VoucherOfferId)
            .HasColumnName("voucher_offer_id")
            .IsRequired();

        userVoucher.HasOne<VoucherOffer>()
            .WithMany()
            .HasForeignKey(x => x.VoucherOfferId)
            .OnDelete(DeleteBehavior.Restrict);

        userVoucher.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(64)
            .IsRequired();

        userVoucher.HasIndex(x => x.Code)
            .IsUnique();

        userVoucher.Property(x => x.PointsSpent)
            .HasColumnName("points_spent")
            .IsRequired();

        userVoucher.Property(x => x.RequestKey)
            .HasColumnName("request_key")
            .HasMaxLength(100)
            .IsRequired();

        userVoucher.HasIndex(x => new { x.UserId, x.RequestKey })
            .IsUnique();

        userVoucher.HasIndex(x => new { x.UserId, x.VoucherOfferId });

        userVoucher.Property(x => x.ClaimedAt)
            .HasColumnName("claimed_at");

        userVoucher.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at");
    }
}
