using GamePortal.Application.Abstractions;

namespace GamePortal.Application.Outbox;

/// <summary>Outbox 메시지(item.grant) 본문. 스키마 변경 시 하위 호환 유지(필드 추가만 허용).</summary>
public sealed record ItemGrantPayload(Guid RequestId, long AccountId, IReadOnlyList<GrantItem> Items, string Reason);
