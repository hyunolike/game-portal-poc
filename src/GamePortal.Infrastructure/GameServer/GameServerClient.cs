using System.Net;
using System.Net.Http.Json;
using GamePortal.Application.Abstractions;

namespace GamePortal.Infrastructure.GameServer;

/// <summary>게임 서버 우편함 API typed HttpClient. 재시도/서킷브레이커는 Resilience 핸들러에서 처리.</summary>
internal sealed class GameServerClient(HttpClient http) : IGameServerClient
{
    public async Task SendItemsToMailboxAsync(
        Guid requestId,
        long accountId,
        IReadOnlyList<GrantItem> items,
        string reason,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "internal/mail/items")
        {
            Content = JsonContent.Create(new { accountId, items, reason }),
        };
        request.Headers.Add("Idempotency-Key", requestId.ToString());

        using var response = await http.SendAsync(request, cancellationToken);

        // 409: 이미 처리된 요청(멱등) → 성공으로 간주
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new GameServerException((int)response.StatusCode, body);
        }
    }
}

public sealed class GameServerException : Exception
{
    public GameServerException(int statusCode, string body)
        : base($"Game server responded {statusCode}: {Truncate(body)}")
    {
        StatusCode = statusCode;
    }

    public GameServerException()
    {
    }

    public GameServerException(string message)
        : base(message)
    {
    }

    public GameServerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public int StatusCode { get; }

    private static string Truncate(string value) => value.Length > 500 ? value[..500] : value;
}
