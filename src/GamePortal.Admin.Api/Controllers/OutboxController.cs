using GamePortal.Admin.Api.Authorization;
using GamePortal.Application.Common;
using GamePortal.Application.Outbox;
using GamePortal.Domain.Outbox;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamePortal.Admin.Api.Controllers;

/// <summary>게임 서버 지급 모니터링 / 실패 건 재처리</summary>
[ApiController]
[Route("api/v1/outbox")]
[Authorize(Policy = AdminPolicies.ContentEditor)]
public sealed class OutboxController(OutboxAdminService outbox) : ControllerBase
{
    [HttpGet("stats")]
    public Task<OutboxStatsDto> Stats(CancellationToken cancellationToken)
        => outbox.GetStatsAsync(cancellationToken);

    [HttpGet]
    public Task<PagedResult<OutboxMessageDto>> GetList(
        [FromQuery] OutboxStatus status = OutboxStatus.Failed,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => outbox.GetListAsync(status, new PageRequest(page, pageSize), cancellationToken);

    [HttpPost("{id:long}/retry")]
    [Authorize(Policy = AdminPolicies.RewardManager)]
    public async Task<IActionResult> Retry(long id, CancellationToken cancellationToken)
    {
        await outbox.RetryAsync(id, cancellationToken);
        return NoContent();
    }
}
