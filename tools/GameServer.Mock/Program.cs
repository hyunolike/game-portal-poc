using System.Collections.Concurrent;

// 게임 서버 우편함 API 목(mock). 로컬에서 Outbox → 게임 서버 흐름과 재시도를 눈으로 확인하는 용도.
// FailureRate 를 올리면 일시 장애 상황(재시도/Dead letter)을 재현할 수 있다.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var apiKey = app.Configuration["ApiKey"] ?? "local-game-server-key";
var failureRate = app.Configuration.GetValue("FailureRate", 0.0);
var processed = new ConcurrentDictionary<string, DateTimeOffset>();

app.MapPost("/internal/mail/items", (HttpRequest request, MailRequest body, ILogger<Program> logger) =>
{
    if (request.Headers["X-Api-Key"] != apiKey)
    {
        return Results.Unauthorized();
    }

    var idempotencyKey = request.Headers["Idempotency-Key"].ToString();
    if (string.IsNullOrEmpty(idempotencyKey))
    {
        return Results.BadRequest("Idempotency-Key required");
    }

#pragma warning disable CA5394 // 목 서버의 장애 주입용 난수
    if (Random.Shared.NextDouble() < failureRate)
#pragma warning restore CA5394
    {
        logger.LogWarning("Injected failure. Key={Key}", idempotencyKey);
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    if (!processed.TryAdd(idempotencyKey, DateTimeOffset.UtcNow))
    {
        logger.LogInformation("Duplicate request ignored. Key={Key}", idempotencyKey);
        return Results.Conflict();
    }

    logger.LogInformation(
        "Mail sent. Account={AccountId} Server={ServerId} Character={CharacterId} Items={Items} Reason={Reason}",
        body.AccountId,
        body.ServerId,
        body.CharacterId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "account-storage",
        string.Join(", ", body.Items.Select(i => $"{i.ItemId}x{i.Quantity}")),
        body.Reason);
    return Results.Ok(new { mailId = Guid.NewGuid() });
});

// 계정의 캐릭터 목록. 계정 번호로 늘 같은 캐릭터를 만들어 돌려준다(목이라 저장하지 않는다).
// 레벨 조건을 확인할 수 있게 계정마다 레벨이 낮은 캐릭터를 하나 둔다.
app.MapGet("/internal/accounts/{accountId:long}/characters", (HttpRequest request, long accountId) =>
{
    if (request.Headers["X-Api-Key"] != apiKey)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(MockCharacters.For(accountId));
});

app.MapGet("/health", () => Results.Ok());

app.Run();

internal sealed record MailRequest(long AccountId, int? ServerId, long? CharacterId, IReadOnlyList<MailItem> Items, string Reason);

internal sealed record CharacterResponse(int ServerId, string ServerName, long CharacterId, string Name, int Level);

internal static class MockCharacters
{
    private static readonly string[] Names = ["별의순례자", "새벽검객", "잿빛궁수", "은빛마도사", "푸른방패", "달그림자"];

    public static IReadOnlyList<CharacterResponse> For(long accountId)
    {
        var seed = (int)(accountId % Names.Length);
        return
        [
            new(1, "아스테라", (accountId * 10) + 1, Names[seed], 40 + (int)(accountId % 30)),
            new(1, "아스테라", (accountId * 10) + 2, Names[(seed + 1) % Names.Length], 8),
            new(2, "벨루나", (accountId * 10) + 3, Names[(seed + 2) % Names.Length], 20 + (int)(accountId % 40)),
        ];
    }
}

internal sealed record MailItem(int ItemId, int Quantity);
