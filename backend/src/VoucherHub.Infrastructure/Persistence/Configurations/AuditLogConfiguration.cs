using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoucherHub.Domain.Entities;

namespace VoucherHub.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        var auditLog = builder;

        auditLog.ToTable("audit_logs");
        auditLog.HasKey(x => x.Id);

        auditLog.Property(x => x.Id)
            .HasColumnName("id");

        auditLog.Property(x => x.ActorUserId)
            .HasColumnName("actor_user_id");

        auditLog.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        auditLog.Property(x => x.Action)
            .HasColumnName("action")
            .HasMaxLength(100)
            .IsRequired();

        auditLog.Property(x => x.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(100)
            .IsRequired();

        auditLog.Property(x => x.EntityId)
            .HasColumnName("entity_id")
            .IsRequired();

        auditLog.Property(x => x.Details)
            .HasColumnName("details")
            .HasColumnType("jsonb");

        auditLog.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        auditLog.HasIndex(x => new { x.EntityType, x.EntityId });
        auditLog.HasIndex(x => x.ActorUserId);
        auditLog.HasIndex(x => x.CreatedAt);
    }
}
