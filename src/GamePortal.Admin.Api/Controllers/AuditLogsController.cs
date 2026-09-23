using GamePortal.Admin.Api.Authorization;
using GamePortal.Application.Auditing;
using GamePortal.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamePortal.Admin.Api.Controllers;

[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Policy = AdminPolicies.RewardManager)]
public sealed class AuditLogsController(AuditLogQueryService auditLogs) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<AuditLogDto>> Search(
        [FromQuery] AuditLogSearch search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => auditLogs.SearchAsync(search, new PageRequest(page, pageSize), cancellationToken);
}
