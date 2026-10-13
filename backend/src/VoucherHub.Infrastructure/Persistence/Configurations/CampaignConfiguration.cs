using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoucherHub.Domain.Entities;

namespace VoucherHub.Infrastructure.Persistence.Configurations;

public sealed class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        var campaign = builder;

        campaign.ToTable("campaigns", table =>
        {
            table.HasCheckConstraint(
                "ck_campaigns_status",
                "status IN ('DRAFT', 'PUBLISHED', 'CANCELLED')");
            table.HasCheckConstraint(
                "ck_campaigns_valid_time_range",
                "ends_at > starts_at");
            table.HasCheckConstraint(
                "ck_campaigns_max_redemptions_positive",
                "max_redemptions_per_user > 0");
        });

        campaign.HasKey(x => x.Id);

        campaign.Property(x => x.Id)
            .HasColumnName("id");

        campaign.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        campaign.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        campaign.Property(x => x.StartsAt)
            .HasColumnName("starts_at");

        campaign.Property(x => x.EndsAt)
            .HasColumnName("ends_at");

        campaign.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue("DRAFT")
            .IsRequired();

        campaign.Property(x => x.MaxRedemptionsPerUser)
            .HasColumnName("max_redemptions_per_user")
            .HasDefaultValue(1)
            .IsRequired();

        campaign.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        campaign.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        campaign.HasIndex(x => new { x.Status, x.StartsAt, x.EndsAt });

        campaign.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        campaign.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
