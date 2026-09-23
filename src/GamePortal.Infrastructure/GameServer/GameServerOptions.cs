using System.ComponentModel.DataAnnotations;

namespace GamePortal.Infrastructure.GameServer;

public sealed class GameServerOptions
{
    public const string SectionName = "GameServer";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>서버 간 인증 키. 운영에서는 Key Vault / Secrets Manager 에서 주입.</summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Range(1, 60)]
    public int TimeoutSeconds { get; set; } = 5;
}
