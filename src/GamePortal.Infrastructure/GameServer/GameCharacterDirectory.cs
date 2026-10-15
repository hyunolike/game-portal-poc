using System.Net;
using System.Net.Http.Json;
using GamePortal.Application.Abstractions;
using GamePortal.Domain.Characters;
using GamePortal.Domain.Common;
using Microsoft.Extensions.Logging;

namespace GamePortal.Infrastructure.GameServer;

/// <summary>
/// 게임 서버 캐릭터 조회 typed HttpClient (Web.Api 전용).
/// 유저가 화면 앞에서 기다리는 요청이라 Worker 의 지급 클라이언트보다 시간 제한을 짧게 두고,
/// 연결할 수 없으면 예외를 흘리지 않고 <see cref="GameServerErrors.Unavailable"/>(503)로 바꾼다.
/// </summary>
internal sealed class GameCharacterDirectory(HttpClient http, ILogger<GameCharacterDirectory> logger) : IGameCharacterDirectory
{
    public async Task<IReadOnlyList<GameCharacter>> GetCharactersAsync(long accountId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.GetAsync($"internal/accounts/{accountId}/characters", cancellationToken);

            // 캐릭터를 하나도 만들지 않은 계정
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return [];
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new GameServerException((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
            }

            var body = await response.Content.ReadFromJsonAsync<List<CharacterResponse>>(cancellationToken) ?? [];
            return body.Select(c => new GameCharacter(c.ServerId, c.ServerName, c.CharacterId, c.Name, c.Level)).ToList();
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // 시간 초과, 서킷 차단, 5xx, 응답 형식 오류를 모두 "지금은 연결할 수 없음" 하나로 다룬다.
            logger.LogWarning(ex, "Game server character lookup failed. AccountId={AccountId}", accountId);
            throw new DomainException(GameServerErrors.Unavailable);
        }
    }

    private sealed record CharacterResponse(int ServerId, string ServerName, long CharacterId, string Name, int Level);
}
