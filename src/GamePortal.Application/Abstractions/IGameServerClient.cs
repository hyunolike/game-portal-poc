namespace GamePortal.Application.Abstractions;

/// <summary>게임 서버(우편함) 연동. 실제 구현은 Infrastructure 의 typed HttpClient.</summary>
public interface IGameServerClient
{
    /// <summary>
    /// 우편함으로 아이템 지급. <paramref name="requestId"/> 는 멱등키 — 재시도로 같은 요청이 여러 번 가도 1회만 지급되어야 한다.
    /// <paramref name="recipient"/> 가 없으면 계정 보관함(모든 캐릭터 공용)으로 보낸다.
    /// </summary>
    Task SendItemsToMailboxAsync(
        Guid requestId,
        long accountId,
        MailRecipient? recipient,
        IReadOnlyList<GrantItem> items,
        string reason,
        CancellationToken cancellationToken);
}

public sealed record GrantItem(int ItemId, int Quantity);

/// <summary>보상을 받을 캐릭터.</summary>
public sealed record MailRecipient(int ServerId, long CharacterId);
