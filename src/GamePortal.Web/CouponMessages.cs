namespace GamePortal.Web;

/// <summary>
/// API 에러 코드 → 유저 안내 문구. 서버 메시지를 그대로 노출하지 않고 코드로 분기한다
/// (문구 변경/다국어 대응을 프론트에서 독립적으로 할 수 있도록).
/// </summary>
public static class CouponMessages
{
    private static readonly Dictionary<string, string> Messages = new(StringComparer.Ordinal)
    {
        ["COUPON_NOT_FOUND"] = "존재하지 않는 쿠폰 번호입니다. 입력하신 번호를 다시 확인해 주세요.",
        ["COUPON_NOT_STARTED"] = "아직 사용 기간이 시작되지 않은 쿠폰입니다.",
        ["COUPON_EXPIRED"] = "사용 기간이 지난 쿠폰입니다.",
        ["COUPON_DISABLED"] = "현재 사용할 수 없는 쿠폰입니다. 공지사항을 확인해 주세요.",
        ["COUPON_ALREADY_USED"] = "이미 사용된 쿠폰 번호입니다.",
        ["COUPON_ALREADY_REDEEMED"] = "이미 보상을 받은 쿠폰입니다. 계정당 1회만 사용할 수 있습니다.",
        ["COUPON_SOLD_OUT"] = "선착순 수량이 모두 소진되었습니다.",
        ["VALIDATION_FAILED"] = "쿠폰 번호 형식이 올바르지 않습니다. 영문과 숫자만 입력해 주세요.",
        ["TOO_MANY_REQUESTS"] = "쿠폰 입력 시도가 너무 많습니다. 1분 후 다시 시도해 주세요.",
    };

    public static string For(string code) =>
        Messages.TryGetValue(code, out var message)
            ? message
            : "일시적인 오류로 쿠폰을 등록하지 못했습니다. 잠시 후 다시 시도해 주세요.";
}
