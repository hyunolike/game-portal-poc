using System.Globalization;
using GamePortal.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace GamePortal.AspNetCore.Auth;

internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public long Id
    {
        get
        {
            var sub = accessor.HttpContext?.User.FindFirst(PortalClaimTypes.Subject)?.Value;
            return long.TryParse(sub, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                ? id
                : throw new UnauthorizedAccessException("인증 주체(sub)를 확인할 수 없습니다.");
        }
    }

    public string Name => accessor.HttpContext?.User.Identity?.Name ?? "anonymous";

    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
