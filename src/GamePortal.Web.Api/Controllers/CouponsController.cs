using GamePortal.Application.Abstractions;
using GamePortal.Application.Common;
using GamePortal.Application.Coupons;
using GamePortal.Web.Api.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GamePortal.Web.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/coupons")]
public sealed class CouponsController(CouponRedeemService coupons, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>쿠폰 사용. 보상은 게임 내 우편함으로 비동기 지급된다.</summary>
    [HttpPost("redeem")]
    [EnableRateLimiting(RateLimitingSetup.CouponRedeemPolicy)]
    [ProducesResponseType<RedeemCouponResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<RedeemCouponResult> Redeem(RedeemCouponRequest request, CancellationToken cancellationToken)
        => coupons.RedeemAsync(currentUser.Id, request, cancellationToken);

    /// <summary>내 쿠폰 사용 내역</summary>
    [HttpGet("history")]
    public Task<PagedResult<MyRedemptionDto>> History(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => coupons.GetMyRedemptionsAsync(currentUser.Id, new PageRequest(page, pageSize), cancellationToken);
}
