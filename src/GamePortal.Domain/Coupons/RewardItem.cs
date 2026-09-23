namespace GamePortal.Domain.Coupons;

/// <summary>쿠폰 보상 아이템. 게임 서버 아이템 테이블의 ItemId 를 참조한다.</summary>
public sealed class RewardItem
{
    public RewardItem(int itemId, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(itemId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quantity, MaxQuantity);

        ItemId = itemId;
        Quantity = quantity;
    }

    public const int MaxQuantity = 99_999;

    public int ItemId { get; private set; }

    public int Quantity { get; private set; }
}
