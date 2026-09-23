using System.Text.Json;
using FluentValidation;
using GamePortal.Application.Abstractions;
using GamePortal.Application.Common;
using GamePortal.Application.Outbox;
using GamePortal.Domain.Common;
using GamePortal.Domain.Coupons;
using GamePortal.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GamePortal.Application.Coupons;

/// <summary>
/// 쿠폰 사용(보상 지급 요청).
///
/// [동시성 설계]
/// 선착순 쿠폰은 오픈 직후 동일 캠페인에 요청이 집중된다. "조회 → 검증 → 증가" 를 애플리케이션에서
/// 하면 Lost Update 로 초과 지급이 발생하므로, 아래 3가지를 DB 에서 원자적으로 보장한다.
///  1) 고유 코드 선점   : UPDATE CouponCodes SET RedeemedBy.. WHERE Id=@id AND RedeemedBy IS NULL
///  2) 총 수량 제한      : UPDATE CouponCampaigns SET RedeemedCount+=1 WHERE Id=@id AND RedeemedCount &lt; Max
///  3) 계정당 1회        : UNIQUE INDEX (CampaignId, AccountId)
/// 위 작업 + 사용 이력 + Outbox 기록을 하나의 트랜잭션으로 묶어 부분 성공이 없도록 한다.
///
/// [게임 서버 연동]
/// 게임 서버 API 호출은 트랜잭션 밖(Worker)에서 Outbox 를 통해 수행한다. → OutboxProcessor
/// </summary>
public sealed class CouponRedeemService(
    IPortalDbContext db,
    TimeProvider clock,
    IValidator<RedeemCouponRequest> validator,
    ILogger<CouponRedeemService> logger)
{
    public async Task<RedeemCouponResult> RedeemAsync(long accountId, RedeemCouponRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var code = CouponCode.Normalize(request.Code);
        var now = clock.GetUtcNow();

        // 1. 사전 검증 (트랜잭션 밖, 읽기 전용) — 실패 케이스 대부분을 락 없이 걸러낸다.
        var coupon = await db.CouponCodes
            .AsNoTracking()
            .Include(c => c.Campaign)
            .SingleOrDefaultAsync(c => c.Code == code, cancellationToken)
            ?? throw new DomainException(CouponErrors.NotFound);

        var campaign = coupon.Campaign;
        campaign.EnsureRedeemable(now);

        if (campaign.Type == CouponType.Unique && coupon.RedeemedByAccountId is not null)
        {
            throw new DomainException(CouponErrors.AlreadyUsed);
        }

        var alreadyRedeemed = await db.CouponRedemptions
            .AnyAsync(r => r.CampaignId == campaign.Id && r.AccountId == accountId, cancellationToken);
        if (alreadyRedeemed)
        {
            throw new DomainException(CouponErrors.AlreadyRedeemedByAccount);
        }

        // 2. 원자적 처리 (트랜잭션). SQL Server 일시적 오류 재시도 정책과 함께 쓰려면 ExecutionStrategy 로 감싸야 한다.
        var strategy = db.Database.CreateExecutionStrategy();
        var redemption = await strategy.ExecuteAsync(
            (Db: db, Campaign: campaign, CouponId: coupon.Id, AccountId: accountId, Now: now),
            static async (state, ct) => await ExecuteRedeemAsync(state.Db, state.Campaign, state.CouponId, state.AccountId, state.Now, ct),
            cancellationToken);

        logger.LogInformation(
            "Coupon redeemed. CampaignId={CampaignId} AccountId={AccountId} RedemptionId={RedemptionId} GrantRequestId={GrantRequestId}",
            campaign.Id, accountId, redemption.Id, redemption.GrantRequestId);

        return new RedeemCouponResult(
            redemption.Id,
            campaign.Name,
            campaign.Rewards.Select(r => new RewardItemDto(r.ItemId, r.Quantity)).ToList(),
            redemption.RedeemedAt);
    }

    public Task<PagedResult<MyRedemptionDto>> GetMyRedemptionsAsync(long accountId, PageRequest page, CancellationToken cancellationToken)
    {
        return db.CouponRedemptions.AsNoTracking()
            .Where(r => r.AccountId == accountId)
            .OrderByDescending(r => r.Id)
            .Join(db.CouponCampaigns, r => r.CampaignId, c => c.Id, (r, c) => new MyRedemptionDto(r.Id, c.Name, r.RedeemedAt))
            .ToPagedResultAsync(page, cancellationToken);
    }

    private static async Task<CouponRedemption> ExecuteRedeemAsync(
        IPortalDbContext db,
        CouponCampaign campaign,
        long couponId,
        long accountId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        db.ChangeTracker.Clear(); // 재시도 시 이전 시도의 추적 엔티티 제거
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        if (campaign.Type == CouponType.Unique)
        {
            var claimed = await db.CouponCodes
                .Where(c => c.Id == couponId && c.RedeemedByAccountId == null)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(c => c.RedeemedByAccountId, accountId)
                          .SetProperty(c => c.RedeemedAt, now),
                    ct);

            if (claimed == 0)
            {
                throw new DomainException(CouponErrors.AlreadyUsed);
            }
        }

        var incremented = await db.CouponCampaigns
            .Where(c => c.Id == campaign.Id && c.IsEnabled && (c.MaxRedemptions == null || c.RedeemedCount < c.MaxRedemptions))
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.RedeemedCount, c => c.RedeemedCount + 1), ct);

        if (incremented == 0)
        {
            // 사전 검증 이후 운영자가 비활성화했거나, 수량이 소진된 경우
            var enabled = await db.CouponCampaigns.Where(c => c.Id == campaign.Id).Select(c => c.IsEnabled).SingleAsync(ct);
            throw new DomainException(enabled ? CouponErrors.SoldOut : CouponErrors.Disabled);
        }

        var grantRequestId = Guid.NewGuid();
        var redemption = CouponRedemption.Create(campaign.Id, couponId, accountId, grantRequestId, now);
        db.CouponRedemptions.Add(redemption);

        var payload = new ItemGrantPayload(
            grantRequestId,
            accountId,
            campaign.Rewards.Select(r => new GrantItem(r.ItemId, r.Quantity)).ToList(),
            $"coupon:{campaign.Id}");
        db.OutboxMessages.Add(OutboxMessage.Create(grantRequestId, OutboxMessageTypes.ItemGrant, JsonSerializer.Serialize(payload), now));

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintViolationException)
        {
            // 같은 계정의 동시 요청(더블 클릭, 매크로) → 트랜잭션 롤백으로 수량 증가도 취소된다.
            throw new DomainException(CouponErrors.AlreadyRedeemedByAccount);
        }

        await tx.CommitAsync(ct);
        return redemption;
    }
}
