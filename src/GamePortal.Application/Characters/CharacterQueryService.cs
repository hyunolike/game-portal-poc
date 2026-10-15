using GamePortal.Application.Abstractions;

namespace GamePortal.Application.Characters;

/// <summary>내 캐릭터 목록. 쿠폰 화면에서 보상을 받을 캐릭터를 고를 때 쓴다.</summary>
public sealed class CharacterQueryService(IGameCharacterDirectory characters)
{
    public async Task<IReadOnlyList<CharacterDto>> GetMyCharactersAsync(long accountId, CancellationToken cancellationToken)
    {
        var list = await characters.GetCharactersAsync(accountId, cancellationToken);
        return list
            .OrderBy(c => c.ServerId)
            .ThenByDescending(c => c.Level)
            .Select(c => new CharacterDto(c.ServerId, c.ServerName, c.CharacterId, c.Name, c.Level))
            .ToList();
    }
}
