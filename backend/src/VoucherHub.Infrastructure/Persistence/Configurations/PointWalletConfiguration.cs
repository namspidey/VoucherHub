using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoucherHub.Domain.Entities;

namespace VoucherHub.Infrastructure.Persistence.Configurations;

public sealed class PointWalletConfiguration : IEntityTypeConfiguration<PointWallet>
{
    public void Configure(EntityTypeBuilder<PointWallet> builder)
    {
        var pointWallet = builder;

        pointWallet.ToTable("point_wallets", table =>
        {
            table.HasCheckConstraint(
                "ck_point_wallets_balance_non_negative",
                "balance >= 0");
        });

        pointWallet.HasKey(x => x.Id);

        pointWallet.Property(x => x.Id)
            .HasColumnName("id");

        pointWallet.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        pointWallet.HasIndex(x => x.UserId)
            .IsUnique();

        pointWallet.HasOne<User>()
            .WithOne()
            .HasForeignKey<PointWallet>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        pointWallet.Property(x => x.Balance)
            .HasColumnName("balance")
            .HasDefaultValue(0L)
            .IsRequired();

        pointWallet.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
