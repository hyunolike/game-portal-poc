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

    /// <summary>보상을 받을 캐릭터. 비우면 계정 보관함으로 받는다.</summary>
    [BindProperty]
    public long? CharacterId { get; set; }

    public IReadOnlyList<GameCharacter> Characters { get; private set; } = [];

    /// <summary>게임 서버에서 캐릭터를 불러오지 못했다. 계정 보관함으로는 계속 받을 수 있다.</summary>
    public bool CharactersUnavailable { get; private set; }

    public RedeemResult? Redeemed { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string? ErrorTraceId { get; private set; }

    public PagedResult<MyRedemption>? History { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        return await LoadPageAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                Redeemed = await api.RedeemCouponAsync(Code, CharacterId, cancellationToken);
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
                ErrorTraceId = ex.StatusCode >= 500 && ex.Code != "GAME_SERVER_UNAVAILABLE" ? ex.TraceId : null;
                logger.LogInformation("Coupon redeem rejected. Code={ErrorCode} Status={Status}", ex.Code, ex.StatusCode);
            }
        }

        return await LoadPageAsync(cancellationToken);
    }

    private async Task<IActionResult> LoadPageAsync(CancellationToken cancellationToken)
    {
        try
        {
            History = await api.GetMyRedemptionsAsync(page: 1, cancellationToken);
        }
        catch (PortalApiException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            return await ReLoginAsync();
        }

        try
        {
            Characters = await api.GetMyCharactersAsync(cancellationToken);
        }
        catch (PortalApiException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            return await ReLoginAsync();
        }
        catch (PortalApiException ex)
        {
            // 캐릭터 목록이 없어도 쿠폰 등록은 막지 않는다. 계정 보관함으로 받을 수 있다.
            CharactersUnavailable = true;
            logger.LogWarning("Character list unavailable. Code={ErrorCode} Status={Status}", ex.Code, ex.StatusCode);
        }

        // 고른 캐릭터가 목록에 없으면(목록을 못 불러온 경우 포함) 선택을 풀어 계정 보관함으로 돌린다.
        if (CharacterId is { } selected && Characters.All(c => c.CharacterId != selected))
        {
            CharacterId = null;
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
