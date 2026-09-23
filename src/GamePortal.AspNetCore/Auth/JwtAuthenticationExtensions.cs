using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace GamePortal.AspNetCore.Auth;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddPortalJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                  ?? throw new InvalidOperationException("Jwt 설정이 없습니다.");
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false; // "sub", "role" 을 그대로 사용
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = PortalClaimTypes.Name,
                    RoleClaimType = PortalClaimTypes.Role,
                };

                if (!string.IsNullOrWhiteSpace(jwt.Authority))
                {
                    o.Authority = jwt.Authority;
                }
                else if (!string.IsNullOrWhiteSpace(jwt.SigningKey))
                {
                    o.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
                }
                else
                {
                    throw new InvalidOperationException("Jwt:Authority 또는 Jwt:SigningKey 중 하나는 필수입니다.");
                }
            });

        services.AddSingleton<DevTokenIssuer>();
        return services;
    }
}

public static class PortalClaimTypes
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string Role = "role";
}
