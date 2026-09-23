using GamePortal.AspNetCore;
using GamePortal.AspNetCore.Auth;
using Microsoft.AspNetCore.Mvc;

namespace GamePortal.Web.Api.Controllers;

/// <summary>
/// 로컬/테스트 전용 플레이어 토큰 발급. 운영에서는 게임 플랫폼 계정 서버가 발급한 토큰을 사용한다.
/// </summary>
[ApiController]
[Route("dev/token")]
public sealed class DevAuthController(DevTokenIssuer issuer, IWebHostEnvironment env) : ControllerBase
{
    public sealed record DevTokenRequest(long AccountId, string? Nickname);

    [HttpPost]
    public IActionResult Issue(DevTokenRequest request)
    {
        if (!env.IsDevelopmentOrTesting())
        {
            return NotFound();
        }

        var token = issuer.Issue(request.AccountId, request.Nickname ?? $"player{request.AccountId}", ["Player"]);
        return Ok(new { accessToken = token });
    }
}
