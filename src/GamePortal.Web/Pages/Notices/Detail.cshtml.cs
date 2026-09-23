using GamePortal.Web.ApiClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamePortal.Web.Pages.Notices;

public sealed class DetailModel(PortalApiClient api) : PageModel
{
    public NoticeDetail Notice { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var notice = await api.GetNoticeAsync(id, cancellationToken);
        if (notice is null)
        {
            return NotFound();
        }

        Notice = notice;
        return Page();
    }
}
