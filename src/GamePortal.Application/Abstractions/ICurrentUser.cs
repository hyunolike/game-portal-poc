namespace GamePortal.Application.Abstractions;

/// <summary>현재 요청의 인증 주체. Web 에서는 플레이어 계정, Admin 에서는 운영자.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>플레이어 AccountId 또는 운영자 OperatorId (JWT sub).</summary>
    long Id { get; }

    string Name { get; }

    string? IpAddress { get; }
}
