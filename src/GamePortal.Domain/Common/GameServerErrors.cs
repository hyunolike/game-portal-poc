namespace GamePortal.Domain.Common;

/// <summary>게임 서버 연동 에러 코드. 클라이언트 계약이므로 코드 값은 변경 금지.</summary>
public static class GameServerErrors
{
    public static readonly DomainError Unavailable = new("GAME_SERVER_UNAVAILABLE", "게임 서버에 연결할 수 없습니다. 잠시 후 다시 시도해 주세요.", ErrorKind.Unavailable);
}
