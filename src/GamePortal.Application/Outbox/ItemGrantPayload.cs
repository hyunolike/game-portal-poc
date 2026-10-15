using System.Text.Json.Serialization;
using GamePortal.Application.Abstractions;

namespace GamePortal.Application.Outbox;

/// <summary>
/// Outbox 메시지(item.grant) 본문. 스키마 변경 시 하위 호환 유지(필드 추가만 허용).
/// ServerId 와 CharacterId 는 나중에 더한 필드라, 그 전에 쌓인 메시지에는 없고 계정 보관함으로 보낸다.
/// </summary>
public sealed record ItemGrantPayload(
    Guid RequestId,
    long AccountId,
    IReadOnlyList<GrantItem> Items,
    string Reason,
    int? ServerId = null,
    long? CharacterId = null)
{
    [JsonIgnore]
    public MailRecipient? Recipient => ServerId is { } serverId && CharacterId is { } characterId
        ? new MailRecipient(serverId, characterId)
        : null;
}
