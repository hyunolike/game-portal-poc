using System.ComponentModel.DataAnnotations;
using GamePortal.Web.ApiClient;
using GamePortal.Web.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamePortal.Web.Pages.Account;

/// <summary>
/// 개발/테스트 전용 로그인. 실서비스에서는 이 페이지 대신 게임 플랫폼 계정 서버(OIDC)로 리다이렉트하고,
/// 콜백에서 받은 토큰으로 <see cref="PortalAuth.SignInWithTokenAsync"/> 를 호출하는 구조가 된다.
/// </summary>
public sealed class LoginModel(PortalApiClient api, IWebHostEnvironment env) : PageModel
{
    [BindProperty]
    [Required(ErrorMessage = "계정 번호를 입력해 주세요.")]
    [Range(1, long.MaxValue, ErrorMessage = "계정 번호는 1 이상의 숫자입니다.")]
    public long? AccountId { get; set; } = 10001;

    [BindProperty]
    [StringLength(20)]
    public string? Nickname { get; set; } = "별의순례자";

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet() => IsDevLoginAllowed() ? Page() : NotFound();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!IsDevLoginAllowed())
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var token = await api.IssueDevTokenAsync(AccountId!.Value, string.IsNullOrWhiteSpace(Nickname) ? $"player{AccountId}" : Nickname.Trim(), cancellationToken);
        await HttpContext.SignInWithTokenAsync(token.AccessToken);

        // Open redirect 방지: 사이트 내부 경로로만 이동
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/");
    }

    private bool IsDevLoginAllowed() => env.IsDevelopment() || env.IsEnvironment("Testing");
}
