using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;

namespace GamePortal.AspNetCore;

/// <summary>
/// X-Forwarded-For 를 믿을 프록시를 설정으로 지정한다.
///
/// 아무 출발지의 헤더나 믿으면 유저가 헤더 한 줄로 IP 를 바꿔 IP 기반 Rate limit 을 피하고,
/// 감사 로그의 IP 도 꾸밀 수 있다. 그래서 프레임워크 기본값(루프백만 신뢰)에서 시작해
/// 로드밸런서나 홈페이지(BFF) 서버가 있는 대역만 <c>ForwardedHeaders:KnownNetworks</c> 로 더한다.
/// </summary>
public static class TrustedForwardedHeaders
{
    public const string SectionName = "ForwardedHeaders";

    public static void Configure(ForwardedHeadersOptions options, IConfiguration configuration)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        var section = configuration.GetSection(SectionName);
        foreach (var network in section.GetSection("KnownNetworks").Get<string[]>() ?? [])
        {
            // 잘못 적은 대역은 조용히 무시하지 않고 기동을 멈춘다. 무시하면 실제 IP 대신 프록시 IP 로 모든 유저가 묶인다.
            if (!Microsoft.AspNetCore.HttpOverrides.IPNetwork.TryParse(network, out var parsed))
            {
                throw new InvalidOperationException($"{SectionName}:KnownNetworks 값 '{network}' 을(를) CIDR 로 읽을 수 없습니다.");
            }

            options.KnownNetworks.Add(parsed);
        }

        foreach (var proxy in section.GetSection("KnownProxies").Get<string[]>() ?? [])
        {
            if (!IPAddress.TryParse(proxy, out var parsed))
            {
                throw new InvalidOperationException($"{SectionName}:KnownProxies 값 '{proxy}' 을(를) IP 주소로 읽을 수 없습니다.");
            }

            options.KnownProxies.Add(parsed);
        }
    }
}
