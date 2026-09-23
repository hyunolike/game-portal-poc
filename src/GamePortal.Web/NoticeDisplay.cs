using GamePortal.Web.ApiClient;

namespace GamePortal.Web;

public static class NoticeDisplay
{
    public static string Label(NoticeCategory category) => category switch
    {
        NoticeCategory.Notice => "공지",
        NoticeCategory.Update => "업데이트",
        NoticeCategory.Event => "이벤트",
        NoticeCategory.Maintenance => "점검",
        _ => "기타",
    };

    public static string CssClass(NoticeCategory category) => "badge badge--" + category.ToString().ToLowerInvariant();

    private static readonly TimeZoneInfo Kst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");

    /// <summary>서버는 UTC 로 저장/응답하고, 표시는 한국 시간으로 한다.</summary>
    public static DateTimeOffset ToKst(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, Kst);

    public static string Date(DateTimeOffset value) => ToKst(value).ToString("yyyy.MM.dd", System.Globalization.CultureInfo.InvariantCulture);

    public static string DateTime(DateTimeOffset value) => ToKst(value).ToString("yyyy.MM.dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
}
