using GamePortal.Web.ApiClient;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamePortal.Web.Pages;

public sealed class IndexModel(PortalApiClient api, ILogger<IndexModel> logger) : PageModel
{
    public IReadOnlyList<NoticeSummary> Notices { get; private set; } = [];

    public NoticeSummary? Maintenance { get; private set; }

    public bool NoticesUnavailable { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await api.GetNoticesAsync(category: null, page: 1, pageSize: 6, cancellationToken);
            Notices = result.Items;

            // 고정된 점검 공지는 메인 상단 배너로 강조 (점검 시간 문의 CS 를 줄이는 가장 싼 방법)
            Maintenance = Notices.FirstOrDefault(n => n.IsPinned && n.Category == NoticeCategory.Maintenance);
        }
        catch (Exception ex) when (ex is PortalApiException or HttpRequestException or TaskCanceledException)
        {
            // 공지 API 장애가 메인 페이지 전체 장애가 되지 않도록 해당 영역만 대체 문구로 보여준다
            logger.LogWarning(ex, "Failed to load notices for home");
            NoticesUnavailable = true;
        }
    }
}
