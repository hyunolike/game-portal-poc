using GamePortal.Domain.Common;
using GamePortal.Domain.Notices;

namespace GamePortal.UnitTests.Domain;

public class NoticeTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public void 예약_게시_공지는_게시_시각_이후에만_노출된다()
    {
        var notice = Notice.Create(NoticeCategory.Maintenance, "정기 점검 안내", "내용", false, Now.AddHours(1), 1, Now);

        Assert.False(notice.IsVisibleAt(Now));
        Assert.True(notice.IsVisibleAt(Now.AddHours(1)));
    }

    [Fact]
    public void 삭제된_공지는_노출되지_않고_수정할_수_없다()
    {
        var notice = Notice.Create(NoticeCategory.Notice, "공지", "내용", false, Now, 1, Now);
        notice.Delete(2, Now);

        Assert.False(notice.IsVisibleAt(Now.AddDays(1)));
        var ex = Assert.Throws<DomainException>(() =>
            notice.Update(NoticeCategory.Notice, "수정", "내용", false, true, Now, 2, Now));
        Assert.Equal(NoticeErrors.AlreadyDeleted.Code, ex.Code);
    }

    [Fact]
    public void 제목은_앞뒤_공백을_제거하고_길이를_제한한다()
    {
        var notice = Notice.Create(NoticeCategory.Notice, "  업데이트  ", "내용", false, Now, 1, Now);
        Assert.Equal("업데이트", notice.Title);

        Assert.Throws<ArgumentException>(() =>
            Notice.Create(NoticeCategory.Notice, new string('가', Notice.TitleMaxLength + 1), "내용", false, Now, 1, Now));
    }
}
