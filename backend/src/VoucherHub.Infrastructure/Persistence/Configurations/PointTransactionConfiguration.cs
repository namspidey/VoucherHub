using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoucherHub.Domain.Entities;

namespace VoucherHub.Infrastructure.Persistence.Configurations;

public sealed class PointTransactionConfiguration : IEntityTypeConfiguration<PointTransaction>
{
    public void Configure(EntityTypeBuilder<PointTransaction> builder)
    {
        var pointTransaction = builder;

        pointTransaction.ToTable("point_transactions", table =>
        {
            table.HasCheckConstraint(
                "ck_point_transactions_type",
                "type IN ('ADMIN_GRANT', 'VOUCHER_REDEMPTION')");
            table.HasCheckConstraint(
                "ck_point_transactions_amount_non_zero",
                "amount <> 0");
            table.HasCheckConstraint(
                "ck_point_transactions_balance_after_non_negative",
                "balance_after >= 0");
            table.HasCheckConstraint(
                "ck_point_transactions_type_details",
                "(type = 'ADMIN_GRANT' AND amount > 0 AND user_voucher_id IS NULL) " +
                "OR (type = 'VOUCHER_REDEMPTION' AND amount < 0 AND user_voucher_id IS NOT NULL)");
        });

        pointTransaction.HasKey(x => x.Id);

        pointTransaction.Property(x => x.Id)
            .HasColumnName("id");

        pointTransaction.Property(x => x.WalletId)
            .HasColumnName("wallet_id")
            .IsRequired();

        pointTransaction.HasOne<PointWallet>()
            .WithMany()
            .HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        pointTransaction.Property(x => x.Type)
            .HasColumnName("type")
            .HasMaxLength(30)
            .IsRequired();

        pointTransaction.Property(x => x.Amount)
            .HasColumnName("amount")
            .IsRequired();

        pointTransaction.Property(x => x.BalanceAfter)
            .HasColumnName("balance_after")
            .IsRequired();

        pointTransaction.Property(x => x.UserVoucherId)
            .HasColumnName("user_voucher_id");

        pointTransaction.HasOne<UserVoucher>()
            .WithMany()
            .HasForeignKey(x => x.UserVoucherId)
            .OnDelete(DeleteBehavior.Restrict);

        pointTransaction.HasIndex(x => x.UserVoucherId)
            .IsUnique();

        pointTransaction.Property(x => x.PerformedBy)
            .HasColumnName("performed_by")
            .IsRequired();

        pointTransaction.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.PerformedBy)
            .OnDelete(DeleteBehavior.Restrict);

        pointTransaction.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasMaxLength(500)
            .IsRequired();

        pointTransaction.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        pointTransaction.HasIndex(x => new { x.WalletId, x.CreatedAt });
    }
}
