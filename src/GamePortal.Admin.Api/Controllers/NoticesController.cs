using GamePortal.Admin.Api.Authorization;
using GamePortal.Application.Common;
using GamePortal.Application.Notices;
using GamePortal.Domain.Notices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamePortal.Admin.Api.Controllers;

[ApiController]
[Route("api/v1/notices")]
[Authorize(Policy = AdminPolicies.AnyStaff)]
public sealed class NoticesController(NoticeAdminService notices) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<AdminNoticeDto>> GetList(
        [FromQuery] NoticeCategory? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => notices.GetListAsync(category, new PageRequest(page, pageSize), cancellationToken);

    [HttpGet("{id:long}")]
    public Task<AdminNoticeDto> Get(long id, CancellationToken cancellationToken)
        => notices.GetAsync(id, cancellationToken);

    [HttpPost]
    [Authorize(Policy = AdminPolicies.ContentEditor)]
    public async Task<IActionResult> Create(CreateNoticeRequest request, CancellationToken cancellationToken)
    {
        var id = await notices.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = AdminPolicies.ContentEditor)]
    public async Task<IActionResult> Update(long id, UpdateNoticeRequest request, CancellationToken cancellationToken)
    {
        await notices.UpdateAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = AdminPolicies.ContentEditor)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await notices.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
