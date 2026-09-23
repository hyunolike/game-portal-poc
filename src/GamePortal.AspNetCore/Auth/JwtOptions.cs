using System.ComponentModel.DataAnnotations;

namespace GamePortal.AspNetCore.Auth;

/// <summary>
/// JWT 검증 설정.
/// - 운영: Authority(OIDC, 게임 플랫폼 계정 서버 / 사내 SSO)를 지정해 JWKS 로 서명 검증
/// - 로컬/테스트: SigningKey(대칭키)로 검증하고 /dev/token 으로 토큰 발급
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string? Authority { get; set; }

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    public string? SigningKey { get; set; }

    public int DevTokenLifetimeMinutes { get; set; } = 60;
}
