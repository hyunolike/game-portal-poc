using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GamePortal.Infrastructure.Outbox;

/// <summary>Outbox 폴링 루프. 처리할 건이 가득 차 있으면 쉬지 않고 연속 처리한다.</summary>
internal sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    TimeProvider clock,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox dispatcher started. BatchSize={BatchSize}", options.Value.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = 0;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
                processed = await processor.ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox dispatch loop failed");
            }

            if (processed < options.Value.BatchSize)
            {
                try
                {
                    await Task.Delay(options.Value.PollingInterval, clock, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
