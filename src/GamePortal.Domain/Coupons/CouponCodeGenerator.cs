using System.Security.Cryptography;

namespace GamePortal.Domain.Coupons;

/// <summary>
/// 고유 쿠폰 코드 생성기.
/// - 혼동 문자(0/O, 1/I) 제외한 32 문자 알파벳 → 12자리 = 60bit 엔트로피
/// - 암호학적 난수 사용 (순차/예측 가능한 코드는 무작위 대입 공격에 취약)
/// </summary>
public static class CouponCodeGenerator
{
    public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // 32자 (0/O, 1/I 제외)

    public const int DefaultLength = 12;

    public static string Generate(int length = DefaultLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 8);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, CouponCode.MaxLength);

        return string.Create(length, 0, static (span, _) =>
        {
            Span<byte> bytes = stackalloc byte[span.Length];
            RandomNumberGenerator.Fill(bytes);
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = Alphabet[bytes[i] & 31];
            }
        });
    }

    /// <summary>중복 없는 코드 N개 생성. DB UNIQUE 인덱스가 최종 방어선이다.</summary>
    public static IReadOnlyCollection<string> GenerateMany(int count, int length = DefaultLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        var set = new HashSet<string>(count, StringComparer.Ordinal);
        while (set.Count < count)
        {
            set.Add(Generate(length));
        }

        return set;
    }

    /// <summary>사용자 배포용 표기. "ABCDEFGH2345" → "ABCD-EFGH-2345".</summary>
    public static string Format(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return string.Join('-', code.Chunk(4).Select(c => new string(c)));
    }
}
