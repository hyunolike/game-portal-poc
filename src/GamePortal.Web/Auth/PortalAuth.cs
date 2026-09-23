using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace GamePortal.Web.Auth;

public static class PortalAuth
{
    public const string AccessTokenName = "access_token";

    public static IServiceCollection AddPortalCookieAuth(this IServiceCollection services, IWebHostEnvironment env)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o =>
            {
                o.Cookie.Name = "gp.session";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.SecurePolicy = env.IsDevelopment() || env.IsEnvironment("Testing")
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                o.LoginPath = "/account/login";
                o.LogoutPath = "/account/logout";
                o.SlidingExpiration = false; // 쿠키 수명을 액세스 토큰 만료에 맞춘다
            });

        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// 게임 플랫폼이 발급한 JWT 로 쿠키 세션을 만든다. 토큰 원문은 암호화된 인증 쿠키 안에만 저장된다.
    /// (서명 검증은 토큰을 실제로 사용하는 Web.Api 가 한다. 여기서는 표시용 클레임만 읽는다)
    /// </summary>
    public static async Task SignInWithTokenAsync(this HttpContext httpContext, string accessToken)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var sub = jwt.Claims.First(c => c.Type == "sub").Value;
        var name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? $"player{sub}";

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, sub), new Claim(ClaimTypes.Name, name)],
            CookieAuthenticationDefaults.AuthenticationScheme);

        var properties = new AuthenticationProperties
        {
            ExpiresUtc = jwt.ValidTo,
            IsPersistent = false,
        };
        properties.StoreTokens([new AuthenticationToken { Name = AccessTokenName, Value = accessToken }]);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);
    }
}
