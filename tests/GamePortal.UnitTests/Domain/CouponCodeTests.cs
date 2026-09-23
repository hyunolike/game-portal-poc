using GamePortal.Domain.Coupons;

namespace GamePortal.UnitTests.Domain;

public class CouponCodeTests
{
    [Theory]
    [InlineData("abcd-efgh-2345", "ABCDEFGH2345")]
    [InlineData("  welcome 2026 ", "WELCOME2026")]
    [InlineData("WELCOME\t2026\n", "WELCOME2026")]
    [InlineData("", "")]
    public void 사용자_입력을_정규화한다(string raw, string expected)
    {
        Assert.Equal(expected, CouponCode.Normalize(raw));
    }

    [Fact]
    public void 비정상적으로_긴_입력은_잘라낸다()
    {
        var normalized = CouponCode.Normalize(new string('A', 10_000));

        Assert.Equal(64, normalized.Length);
    }

    [Fact]
    public void 생성된_코드는_지정한_길이와_알파벳을_따른다()
    {
        var code = CouponCodeGenerator.Generate();

        Assert.Equal(CouponCodeGenerator.DefaultLength, code.Length);
        Assert.All(code, ch => Assert.Contains(ch, CouponCodeGenerator.Alphabet));
    }

    [Fact]
    public void 알파벳은_혼동_문자를_포함하지_않는다()
    {
        Assert.Equal(32, CouponCodeGenerator.Alphabet.Length);
        Assert.Equal(32, CouponCodeGenerator.Alphabet.Distinct().Count());
        Assert.DoesNotContain('0', CouponCodeGenerator.Alphabet);
        Assert.DoesNotContain('O', CouponCodeGenerator.Alphabet);
        Assert.DoesNotContain('1', CouponCodeGenerator.Alphabet);
        Assert.DoesNotContain('I', CouponCodeGenerator.Alphabet);
    }

    [Fact]
    public void 대량_생성시_중복이_없다()
    {
        var codes = CouponCodeGenerator.GenerateMany(50_000);

        Assert.Equal(50_000, codes.Count);
        Assert.Equal(50_000, codes.Distinct().Count());
    }

    [Fact]
    public void 배포용_포맷은_4자리마다_하이픈을_넣는다()
    {
        Assert.Equal("ABCD-EFGH-2345", CouponCodeGenerator.Format("ABCDEFGH2345"));
        Assert.Equal("ABCDEFGH2345", CouponCode.Normalize(CouponCodeGenerator.Format("ABCDEFGH2345")));
    }
}
