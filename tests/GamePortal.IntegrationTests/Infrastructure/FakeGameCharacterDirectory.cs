using System.Collections.Concurrent;
using GamePortal.Application.Abstractions;
using GamePortal.Domain.Characters;
using GamePortal.Domain.Common;

namespace GamePortal.IntegrationTests.Infrastructure;

/// <summary>
/// 게임 서버 캐릭터 조회 대역. 테스트마다 계정 번호가 달라 서로 간섭하지 않는다.
/// 따로 정하지 않은 계정에는 캐릭터가 없다.
/// </summary>
public sealed class FakeGameCharacterDirectory : IGameCharacterDirectory
{
    private readonly ConcurrentDictionary<long, IReadOnlyList<GameCharacter>> _characters = new();
    private readonly ConcurrentDictionary<long, bool> _unavailable = new();

    public void Set(long accountId, params GameCharacter[] characters) => _characters[accountId] = characters;

    /// <summary>이 계정을 조회하면 게임 서버에 연결할 수 없는 것처럼 실패한다.</summary>
    public void MakeUnavailable(long accountId) => _unavailable[accountId] = true;

    public Task<IReadOnlyList<GameCharacter>> GetCharactersAsync(long accountId, CancellationToken cancellationToken)
    {
        if (_unavailable.ContainsKey(accountId))
        {
            throw new DomainException(GameServerErrors.Unavailable);
        }

        return Task.FromResult(_characters.GetValueOrDefault(accountId) ?? []);
    }
}
