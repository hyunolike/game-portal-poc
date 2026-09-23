using GamePortal.Web.ApiClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamePortal.Web.Pages.Notices;

public sealed class IndexModel(PortalApiClient api) : PageModel
{
    public const int PageSize = 15;

    [BindProperty(SupportsGet = true)]
    public NoticeCategory? Category { get; set; }

    [BindProperty(Name = "page", SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public PagedResult<NoticeSummary> Result { get; private set; } = new([], 1, PageSize, 0, 0);

    public static IReadOnlyList<NoticeCategory?> Tabs { get; } =
        [null, NoticeCategory.Notice, NoticeCategory.Update, NoticeCategory.Event, NoticeCategory.Maintenance];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        CurrentPage = Math.Max(CurrentPage, 1);
        Result = await api.GetNoticesAsync(Category, CurrentPage, PageSize, cancellationToken);
    }

    public string PageUrl(int page, NoticeCategory? category)
    {
        var url = $"/notices?page={page}";
        return category is null ? url : url + $"&category={category}";
    }
}
