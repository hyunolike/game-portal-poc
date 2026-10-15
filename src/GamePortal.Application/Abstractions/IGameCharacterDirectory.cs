using GamePortal.Domain.Characters;

namespace GamePortal.Application.Abstractions;

/// <summary>
/// 게임 서버의 캐릭터 조회(읽기 전용). 쿠폰을 받을 캐릭터를 고르고 확인하는 데 쓴다.
/// 지급(쓰기)은 여전히 Outbox 와 Worker 가 맡는다. 게임 서버에 연결할 수 없으면
/// <see cref="Domain.Common.GameServerErrors.Unavailable"/> 로 실패한다.
/// </summary>
public interface IGameCharacterDirectory
{
    Task<IReadOnlyList<GameCharacter>> GetCharactersAsync(long accountId, CancellationToken cancellationToken);
}
