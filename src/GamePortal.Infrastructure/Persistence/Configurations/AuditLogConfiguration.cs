using GamePortal.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GamePortal.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OperatorName).HasMaxLength(100);
        builder.Property(x => x.Action).HasMaxLength(20).IsUnicode(false);
        builder.Property(x => x.EntityName).HasMaxLength(100).IsUnicode(false);
        builder.Property(x => x.EntityId).HasMaxLength(50).IsUnicode(false);
        builder.Property(x => x.Changes).HasMaxLength(-1);
        builder.Property(x => x.IpAddress).HasMaxLength(45).IsUnicode(false);

        builder.HasIndex(x => new { x.EntityName, x.EntityId });
        builder.HasIndex(x => new { x.OperatorId, x.OccurredAt });
    }
}
