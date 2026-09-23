namespace GamePortal.Domain.Coupons;

public class CouponCode
{
    public const int MaxLength = 20;

    private CouponCode()
    {
        Code = string.Empty;
    }

    public long Id { get; private set; }

    public long CampaignId { get; private set; }

    public CouponCampaign Campaign { get; private set; } = null!;

    /// <summary>정규화된 코드(대문자, 하이픈/공백 제거). UNIQUE 인덱스.</summary>
    public string Code { get; private set; }

    /// <summary>고유 코드(<see cref="CouponType.Unique"/>)일 때만 사용. 조건부 UPDATE 로 선점한다.</summary>
    public long? RedeemedByAccountId { get; private set; }

    public DateTimeOffset? RedeemedAt { get; private set; }

    public static CouponCode Create(long campaignId, string code) => new()
    {
        CampaignId = campaignId,
        Code = Normalize(code),
    };

    /// <summary>
    /// 사용자 입력 정규화. "abcd-efgh 1234" → "ABCDEFGH1234".
    /// 저장/조회 모두 이 함수를 거쳐야 인덱스를 탈 수 있다.
    /// </summary>
    public static string Normalize(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        Span<char> buffer = stackalloc char[Math.Min(raw.Length, 64)];
        var length = 0;
        foreach (var ch in raw)
        {
            if (ch is '-' or ' ' || char.IsWhiteSpace(ch))
            {
                continue;
            }

            if (length == buffer.Length)
            {
                break;
            }

            buffer[length++] = char.ToUpperInvariant(ch);
        }

        return new string(buffer[..length]);
    }
}
