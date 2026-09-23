using GamePortal.Domain.Notices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GamePortal.Infrastructure.Persistence.Configurations;

internal sealed class NoticeConfiguration : IEntityTypeConfiguration<Notice>
{
    public void Configure(EntityTypeBuilder<Notice> builder)
    {
        builder.ToTable("Notices");
        builder.HasKey(x => x.Id);

        // HiLo: INSERT 전에 Id 가 확정되므로 감사 로그를 같은 SaveChanges(=같은 트랜잭션)에 기록할 수 있다.
        builder.Property(x => x.Id).UseHiLo("NoticeIdSeq");

        builder.Property(x => x.Title).HasMaxLength(Notice.TitleMaxLength).IsRequired();
        builder.Property(x => x.Content).HasMaxLength(-1).IsRequired(); // nvarchar(max)
        builder.Property(x => x.Category).HasConversion<byte>();

        // 웹 목록 조회 전용 필터드 인덱스 (삭제/미게시 글 제외, 커버링)
        builder.HasIndex(x => new { x.IsPinned, x.PublishAt })
            .HasDatabaseName("IX_Notices_Visible")
            .HasFilter("[IsDeleted] = 0 AND [IsPublished] = 1")
            .IsDescending(true, true)
            .IncludeProperties(x => new { x.Category, x.Title });
    }
}
