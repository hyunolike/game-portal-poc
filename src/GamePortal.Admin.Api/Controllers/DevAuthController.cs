using GamePortal.AspNetCore;
using GamePortal.AspNetCore.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamePortal.Admin.Api.Controllers;

/// <summary>로컬/테스트 전용 운영자 토큰 발급. 운영에서는 사내 SSO(OIDC) 토큰을 사용한다.</summary>
[ApiController]
[AllowAnonymous]
[Route("dev/token")]
public sealed class DevAuthController(DevTokenIssuer issuer, IWebHostEnvironment env) : ControllerBase
{
    public sealed record DevOperatorTokenRequest(long OperatorId, string Name, string[] Roles);

    [HttpPost]
    public IActionResult Issue(DevOperatorTokenRequest request)
    {
        if (!env.IsDevelopmentOrTesting())
        {
            return NotFound();
        }

        return Ok(new { accessToken = issuer.Issue(request.OperatorId, request.Name, request.Roles) });
    }
}
