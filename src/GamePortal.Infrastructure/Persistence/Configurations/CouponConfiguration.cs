using GamePortal.Domain.Coupons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GamePortal.Infrastructure.Persistence.Configurations;

internal sealed class CouponCampaignConfiguration : IEntityTypeConfiguration<CouponCampaign>
{
    public void Configure(EntityTypeBuilder<CouponCampaign> builder)
    {
        builder.ToTable("CouponCampaigns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseHiLo("CouponCampaignIdSeq");
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Type).HasConversion<byte>();

        // 보상 목록은 캠페인과 생명주기가 같고 단독 조회가 없으므로 JSON 컬럼으로 저장
        builder.OwnsMany(x => x.Rewards, r => r.ToJson());
        builder.Navigation(x => x.Rewards).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class CouponCodeConfiguration : IEntityTypeConfiguration<CouponCode>
{
    public void Configure(EntityTypeBuilder<CouponCode> builder)
    {
        builder.ToTable("CouponCodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(CouponCode.MaxLength).IsUnicode(false).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.CampaignId);

        builder.HasOne(x => x.Campaign)
            .WithMany()
            .HasForeignKey(x => x.CampaignId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CouponRedemptionConfiguration : IEntityTypeConfiguration<CouponRedemption>
{
    public void Configure(EntityTypeBuilder<CouponRedemption> builder)
    {
        builder.ToTable("CouponRedemptions");
        builder.HasKey(x => x.Id);

        // 계정당 1회 사용의 최종 방어선
        builder.HasIndex(x => new { x.CampaignId, x.AccountId }).IsUnique();
        // "내 쿠폰 사용 내역" / CS 계정 조회
        builder.HasIndex(x => new { x.AccountId, x.Id }).IsDescending(false, true);
        builder.HasIndex(x => x.GrantRequestId).IsUnique();

        builder.HasOne<CouponCampaign>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CouponCode>().WithMany().HasForeignKey(x => x.CouponCodeId).OnDelete(DeleteBehavior.Restrict);
    }
}
