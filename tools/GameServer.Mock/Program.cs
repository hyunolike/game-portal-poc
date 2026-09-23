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
        "Mail sent. Account={AccountId} Items={Items} Reason={Reason}",
        body.AccountId,
        string.Join(", ", body.Items.Select(i => $"{i.ItemId}x{i.Quantity}")),
        body.Reason);
    return Results.Ok(new { mailId = Guid.NewGuid() });
});

app.MapGet("/health", () => Results.Ok());

app.Run();

internal sealed record MailRequest(long AccountId, IReadOnlyList<MailItem> Items, string Reason);

internal sealed record MailItem(int ItemId, int Quantity);
