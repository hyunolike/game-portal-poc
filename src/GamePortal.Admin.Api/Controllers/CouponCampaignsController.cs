using System.Globalization;
using System.Text;
using GamePortal.Admin.Api.Authorization;
using GamePortal.Application.Common;
using GamePortal.Application.Coupons;
using GamePortal.Domain.Coupons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamePortal.Admin.Api.Controllers;

[ApiController]
[Route("api/v1/coupon-campaigns")]
[Authorize(Policy = AdminPolicies.AnyStaff)]
public sealed class CouponCampaignsController(CouponCampaignAdminService campaigns) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<CampaignSummaryDto>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => campaigns.GetListAsync(new PageRequest(page, pageSize), cancellationToken);

    [HttpGet("{id:long}")]
    public Task<CampaignDetailDto> Get(long id, CancellationToken cancellationToken)
        => campaigns.GetAsync(id, cancellationToken);

    /// <summary>캠페인 생성 + 코드 발행 (공용 코드 1개 또는 고유 코드 N개)</summary>
    [HttpPost]
    [Authorize(Policy = AdminPolicies.RewardManager)]
    public async Task<IActionResult> Create(CreateCampaignRequest request, CancellationToken cancellationToken)
    {
        var id = await campaigns.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    /// <summary>긴급 사용 중지 (코드 유출, 보상 설정 오류 등)</summary>
    [HttpPost("{id:long}/disable")]
    [Authorize(Policy = AdminPolicies.RewardManager)]
    public async Task<IActionResult> Disable(long id, CancellationToken cancellationToken)
    {
        await campaigns.SetEnabledAsync(id, enabled: false, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/enable")]
    [Authorize(Policy = AdminPolicies.RewardManager)]
    public async Task<IActionResult> Enable(long id, CancellationToken cancellationToken)
    {
        await campaigns.SetEnabledAsync(id, enabled: true, cancellationToken);
        return NoContent();
    }

    /// <summary>고유 코드 CSV 다운로드 (제휴처 전달용). 대량 데이터이므로 스트리밍.</summary>
    [HttpGet("{id:long}/codes.csv")]
    [Authorize(Policy = AdminPolicies.RewardManager)]
    public async Task ExportCodes(long id, CancellationToken cancellationToken)
    {
        await campaigns.GetAsync(id, cancellationToken); // 존재 확인 (404)

        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.ContentDisposition = $"attachment; filename=coupon-codes-{id}.csv";

        await using var writer = new StreamWriter(Response.Body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        await writer.WriteLineAsync("code,redeemed_by_account_id,redeemed_at");
        await foreach (var row in campaigns.StreamCodesAsync(id).WithCancellation(cancellationToken))
        {
            await writer.WriteLineAsync(string.Create(
                CultureInfo.InvariantCulture,
                $"{CouponCodeGenerator.Format(row.Code)},{row.RedeemedByAccountId},{row.RedeemedAt:O}"));
        }
    }
}
