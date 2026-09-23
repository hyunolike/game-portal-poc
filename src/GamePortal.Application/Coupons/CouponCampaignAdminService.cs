using FluentValidation;
using GamePortal.Application.Abstractions;
using GamePortal.Application.Common;
using GamePortal.Domain.Common;
using GamePortal.Domain.Coupons;
using GamePortal.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GamePortal.Application.Coupons;

/// <summary>운영툴 쿠폰 캠페인 관리 (발행, 조회, 중지, 코드 추출, CS 조회).</summary>
public sealed class CouponCampaignAdminService(
    IPortalDbContext db,
    ICouponCodeBulkWriter bulkWriter,
    ICurrentUser currentUser,
    TimeProvider clock,
    IValidator<CreateCampaignRequest> validator,
    ILogger<CouponCampaignAdminService> logger)
{
    public static readonly DomainError DuplicatedCode = new("COUPON_CODE_DUPLICATED", "이미 존재하는 쿠폰 코드입니다.", ErrorKind.Conflict);

    public async Task<long> CreateAsync(CreateCampaignRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var now = clock.GetUtcNow();

        var campaign = CouponCampaign.Create(
            request.Name,
            request.Type,
            request.StartsAt,
            request.EndsAt,
            request.MaxRedemptions,
            request.Rewards.Select(r => new RewardItem(r.ItemId, r.Quantity)),
            currentUser.Id,
            now);

        // 코드는 트랜잭션 밖에서 미리 생성 (CPU 작업으로 트랜잭션/락 시간을 늘리지 않기 위해)
        var uniqueCodes = request.Type == CouponType.Unique
            ? CouponCodeGenerator.GenerateMany(request.UniqueCodeCount!.Value)
            : null;

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async ct =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            db.CouponCampaigns.Add(campaign);
            await db.SaveChangesAsync(ct);

            try
            {
                if (request.Type == CouponType.Shared)
                {
                    db.CouponCodes.Add(CouponCode.Create(campaign.Id, request.SharedCode!));
                    await db.SaveChangesAsync(ct);
                }
                else
                {
                    await bulkWriter.WriteAsync(campaign.Id, uniqueCodes!, ct);
                }
            }
            catch (UniqueConstraintViolationException)
            {
                throw new DomainException(DuplicatedCode);
            }

            await tx.CommitAsync(ct);
        }, cancellationToken);

        logger.LogInformation(
            "Coupon campaign created. CampaignId={CampaignId} Type={Type} Codes={CodeCount} OperatorId={OperatorId}",
            campaign.Id, campaign.Type, uniqueCodes?.Count ?? 1, currentUser.Id);

        return campaign.Id;
    }

    public Task<PagedResult<CampaignSummaryDto>> GetListAsync(PageRequest page, CancellationToken cancellationToken)
    {
        return db.CouponCampaigns.AsNoTracking()
            .OrderByDescending(c => c.Id)
            .Select(c => new CampaignSummaryDto(c.Id, c.Name, c.Type, c.StartsAt, c.EndsAt, c.MaxRedemptions, c.RedeemedCount, c.IsEnabled))
            .ToPagedResultAsync(page, cancellationToken);
    }

    public async Task<CampaignDetailDto> GetAsync(long id, CancellationToken cancellationToken)
    {
        var campaign = await db.CouponCampaigns.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
                       ?? throw new DomainException(CouponErrors.CampaignNotFound);
        var codeCount = await db.CouponCodes.CountAsync(c => c.CampaignId == id, cancellationToken);

        return new CampaignDetailDto(
            campaign.Id,
            campaign.Name,
            campaign.Type,
            campaign.StartsAt,
            campaign.EndsAt,
            campaign.MaxRedemptions,
            campaign.RedeemedCount,
            campaign.IsEnabled,
            campaign.Rewards.Select(r => new RewardItemDto(r.ItemId, r.Quantity)).ToList(),
            codeCount,
            campaign.CreatedBy,
            campaign.CreatedAt);
    }

    public async Task SetEnabledAsync(long id, bool enabled, CancellationToken cancellationToken)
    {
        var campaign = await db.CouponCampaigns.SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
                       ?? throw new DomainException(CouponErrors.CampaignNotFound);

        if (enabled)
        {
            campaign.Enable();
        }
        else
        {
            campaign.Disable();
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogWarning("Coupon campaign {Action}. CampaignId={CampaignId} OperatorId={OperatorId}",
            enabled ? "enabled" : "disabled", id, currentUser.Id);
    }

    /// <summary>
    /// 코드 CSV 추출(제휴사 전달용). 10만 건을 메모리에 올리지 않도록 스트리밍한다.
    /// </summary>
    public IAsyncEnumerable<CouponCodeExportRow> StreamCodesAsync(long campaignId)
    {
        return db.CouponCodes.AsNoTracking()
            .Where(c => c.CampaignId == campaignId)
            .OrderBy(c => c.Id)
            .Select(c => new CouponCodeExportRow(c.Code, c.RedeemedByAccountId, c.RedeemedAt))
            .AsAsyncEnumerable();
    }

    /// <summary>CS 조회: "쿠폰 썼는데 아이템이 안 왔어요" → 사용 이력 + 게임 서버 지급 상태를 함께 본다.</summary>
    public Task<PagedResult<RedemptionAdminDto>> SearchRedemptionsAsync(
        long? accountId,
        long? campaignId,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var query = db.CouponRedemptions.AsNoTracking();
        if (accountId is not null)
        {
            query = query.Where(r => r.AccountId == accountId);
        }

        if (campaignId is not null)
        {
            query = query.Where(r => r.CampaignId == campaignId);
        }

        return (from r in query
                join c in db.CouponCampaigns on r.CampaignId equals c.Id
                join code in db.CouponCodes on r.CouponCodeId equals code.Id
                join o in db.OutboxMessages on r.GrantRequestId equals o.MessageId into outbox
                from o in outbox.DefaultIfEmpty()
                orderby r.Id descending
                select new RedemptionAdminDto(
                    r.Id,
                    r.CampaignId,
                    c.Name,
                    code.Code,
                    r.AccountId,
                    r.RedeemedAt,
                    r.GrantRequestId,
                    o == null ? null : (o.Status == OutboxStatus.Processed ? "Processed" : o.Status == OutboxStatus.Failed ? "Failed" : "Pending")))
            .ToPagedResultAsync(page, cancellationToken);
    }
}
