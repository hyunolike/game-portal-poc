namespace GamePortal.Web;

/// <summary>
/// 보상 아이템 표시 정보. 실서비스에서는 게임 데이터 테이블(기획 데이터)에서 빌드 시 생성하거나
/// 별도 메타 API 로 받아온다. PoC 에서는 고정 목록.
/// </summary>
public static class ItemCatalog
{
    private static readonly Dictionary<int, string> Names = new()
    {
        [1001] = "에테르 주화",
        [1002] = "강화석",
        [2001] = "경험치 부스터 (1시간)",
        [2002] = "한정판 망토 「별의 순례자」",
        [3001] = "탈것 소환권",
    };

    public static string NameOf(int itemId) => Names.TryGetValue(itemId, out var name) ? name : $"아이템 #{itemId}";
}
