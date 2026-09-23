using GamePortal.Application.Common;
using GamePortal.Application.Notices;
using GamePortal.Domain.Notices;
using Microsoft.AspNetCore.Mvc;

namespace GamePortal.Web.Api.Controllers;

[ApiController]
[Route("api/v1/notices")]
public sealed class NoticesController(NoticeQueryService notices) : ControllerBase
{
    /// <summary>공지 목록 (고정글 우선, 최신순)</summary>
    [HttpGet]
    [ResponseCache(Duration = 10, Location = ResponseCacheLocation.Any)] // CDN/브라우저 단 짧은 캐시
    public Task<PagedResult<NoticeSummaryDto>> GetList(
        [FromQuery] NoticeCategory? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => notices.GetListAsync(category, new PageRequest(page, pageSize), cancellationToken);

    [HttpGet("{id:long}")]
    [ResponseCache(Duration = 10, Location = ResponseCacheLocation.Any)]
    public Task<NoticeDetailDto> Get(long id, CancellationToken cancellationToken)
        => notices.GetAsync(id, cancellationToken);
}
