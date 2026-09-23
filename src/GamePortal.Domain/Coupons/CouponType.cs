namespace GamePortal.Domain.Coupons;

public enum CouponType
{
    /// <summary>
    /// 공용 코드 (예: 방송/커뮤니티 배포용 "WELCOME2026").
    /// 코드 1개를 여러 계정이 사용, 계정당 1회, 총 수량 제한 가능.
    /// </summary>
    Shared = 1,

    /// <summary>고유 코드 (예: 패키지 동봉, 제휴처 배포). 코드 1개당 1회만 사용 가능.</summary>
    Unique = 2,
}
