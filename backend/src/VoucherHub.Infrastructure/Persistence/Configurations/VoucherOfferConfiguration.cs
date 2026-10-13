using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoucherHub.Domain.Entities;

namespace VoucherHub.Infrastructure.Persistence.Configurations;

public sealed class VoucherOfferConfiguration : IEntityTypeConfiguration<VoucherOffer>
{
    public void Configure(EntityTypeBuilder<VoucherOffer> builder)
    {
        var voucherOffer = builder;

        voucherOffer.ToTable("voucher_offers", table =>
        {
            table.HasCheckConstraint(
                "ck_voucher_offers_points_cost_positive",
                "points_cost > 0");
            table.HasCheckConstraint(
                "ck_voucher_offers_total_quantity_positive",
                "total_quantity > 0");
            table.HasCheckConstraint(
                "ck_voucher_offers_issued_quantity_range",
                "issued_quantity >= 0 AND issued_quantity <= total_quantity");
        });

        voucherOffer.HasKey(x => x.Id);

        voucherOffer.Property(x => x.Id)
            .HasColumnName("id");

        voucherOffer.Property(x => x.CampaignId)
            .HasColumnName("campaign_id")
            .IsRequired();

        voucherOffer.HasOne<Campaign>()
            .WithMany()
            .HasForeignKey(x => x.CampaignId)
            .OnDelete(DeleteBehavior.Restrict);

        voucherOffer.HasIndex(x => x.CampaignId);

        voucherOffer.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        voucherOffer.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        voucherOffer.Property(x => x.PointsCost)
            .HasColumnName("points_cost")
            .IsRequired();

        voucherOffer.Property(x => x.TotalQuantity)
            .HasColumnName("total_quantity")
            .IsRequired();

        voucherOffer.Property(x => x.IssuedQuantity)
            .HasColumnName("issued_quantity")
            .HasDefaultValue(0)
            .IsRequired();

        voucherOffer.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at");

        voucherOffer.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        voucherOffer.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
