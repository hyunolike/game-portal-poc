using System.ComponentModel.DataAnnotations;

namespace GamePortal.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    [Range(1, 500)]
    public int BatchSize { get; set; } = 50;

    [Range(typeof(TimeSpan), "00:00:00.100", "00:01:00")]
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>한 번 가져간 메시지를 다른 Worker 가 가져가지 못하는 시간. 배치 처리 시간보다 충분히 길어야 한다.</summary>
    [Range(typeof(TimeSpan), "00:00:10", "00:30:00")]
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(2);
}
