using GamePortal.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GamePortal.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(x => x.Payload).HasMaxLength(-1).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<byte>();
        builder.HasIndex(x => x.MessageId).IsUnique();

        // Worker 폴링 전용 (처리 대기 건만 인덱싱 → 처리 완료 건이 쌓여도 인덱스 크기 유지)
        builder.HasIndex(x => new { x.NextAttemptAt, x.Id })
            .HasDatabaseName("IX_OutboxMessages_Pending")
            .HasFilter("[Status] = 0")
            .IncludeProperties(x => x.LockedUntil);
    }
}
