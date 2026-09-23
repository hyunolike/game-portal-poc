using System.ComponentModel.DataAnnotations;
using GamePortal.Web.ApiClient;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamePortal.Web.Pages.Coupon;

public sealed class IndexModel(PortalApiClient api, ILogger<IndexModel> logger) : PageModel
{
    [BindProperty]
    [Required(ErrorMessage = "쿠폰 번호를 입력해 주세요.")]
    [StringLength(30, ErrorMessage = "쿠폰 번호가 너무 깁니다.")]
    public string Code { get; set; } = string.Empty;

    public RedeemResult? Redeemed { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string? ErrorTraceId { get; private set; }

    public PagedResult<MyRedemption>? History { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        return await LoadHistoryAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                Redeemed = await api.RedeemCouponAsync(Code, cancellationToken);
                Code = string.Empty;
                ModelState.Clear();
            }
            catch (PortalApiException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
            {
                return await ReLoginAsync();
            }
            catch (PortalApiException ex)
            {
                ErrorMessage = CouponMessages.For(ex.Code);
                // 원인 불명 오류일 때만 CS 문의용 추적 ID 를 보여준다
                ErrorTraceId = ex.StatusCode >= 500 ? ex.TraceId : null;
                logger.LogInformation("Coupon redeem rejected. Code={ErrorCode} Status={Status}", ex.Code, ex.StatusCode);
            }
        }

        return await LoadHistoryAsync(cancellationToken);
    }

    private async Task<IActionResult> LoadHistoryAsync(CancellationToken cancellationToken)
    {
        try
        {
            History = await api.GetMyRedemptionsAsync(page: 1, cancellationToken);
        }
        catch (PortalApiException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            return await ReLoginAsync();
        }

        return Page();
    }

    /// <summary>쿠키는 살아있는데 API 토큰이 만료/폐기된 경우 → 세션을 정리하고 다시 로그인</summary>
    private async Task<IActionResult> ReLoginAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/account/login?returnUrl=%2Fcoupon");
    }
}
