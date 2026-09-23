using GamePortal.Admin.Api.Authorization;
using GamePortal.Application.Common;
using GamePortal.Application.Coupons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamePortal.Admin.Api.Controllers;

/// <summary>CS 조회: 계정별 쿠폰 사용 이력 + 게임 서버 지급 상태</summary>
[ApiController]
[Route("api/v1/coupon-redemptions")]
[Authorize(Policy = AdminPolicies.AnyStaff)]
public sealed class CouponRedemptionsController(CouponCampaignAdminService campaigns) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<RedemptionAdminDto>> Search(
        [FromQuery] long? accountId,
        [FromQuery] long? campaignId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => campaigns.SearchRedemptionsAsync(accountId, campaignId, new PageRequest(page, pageSize), cancellationToken);
}
