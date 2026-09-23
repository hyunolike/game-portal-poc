using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GamePortal.AspNetCore.Auth;

/// <summary>로컬 개발/통합 테스트 전용 토큰 발급기. 운영 환경에서는 엔드포인트 자체가 매핑되지 않는다.</summary>
public sealed class DevTokenIssuer(IOptions<JwtOptions> options, TimeProvider clock)
{
    public string Issue(long subjectId, string name, IEnumerable<string> roles)
    {
        var jwt = options.Value;
        if (string.IsNullOrWhiteSpace(jwt.SigningKey))
        {
            throw new InvalidOperationException("SigningKey 가 없으면 개발용 토큰을 발급할 수 없습니다.");
        }

        var claims = new List<Claim>
        {
            new(PortalClaimTypes.Subject, subjectId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new(PortalClaimTypes.Name, name),
        };
        claims.AddRange(roles.Select(r => new Claim(PortalClaimTypes.Role, r)));

        var now = clock.GetUtcNow().UtcDateTime;
        var token = new JwtSecurityToken(
            jwt.Issuer,
            jwt.Audience,
            claims,
            notBefore: now,
            expires: now.AddMinutes(jwt.DevTokenLifetimeMinutes),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
