namespace GamePortal.Domain.Characters;

/// <summary>
/// 게임 서버가 알려 주는 캐릭터. 원본은 게임 DB 에 있고 포털은 저장하지 않는다.
/// 쿠폰 보상을 받을 곳을 고를 때와, 레벨 같은 대상 조건을 확인할 때 조회한다.
/// </summary>
public sealed record GameCharacter(int ServerId, string ServerName, long CharacterId, string Name, int Level);
