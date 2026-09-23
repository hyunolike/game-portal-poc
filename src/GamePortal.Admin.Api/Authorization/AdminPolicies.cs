namespace GamePortal.Admin.Api.Authorization;

/// <summary>
/// 운영툴 권한. 최소 권한 원칙: CS 는 조회만, 운영자는 콘텐츠 관리, 관리자는 재화/보상 관련 작업.
/// </summary>
public static class AdminRoles
{
    public const string Admin = "Admin";
    public const string Operator = "Operator";
    public const string CustomerSupport = "CS";
}

public static class AdminPolicies
{
    /// <summary>운영툴 접근 가능한 모든 역할 (조회)</summary>
    public const string AnyStaff = nameof(AnyStaff);

    /// <summary>콘텐츠(공지) 편집</summary>
    public const string ContentEditor = nameof(ContentEditor);

    /// <summary>보상/재화 관련 (쿠폰 발행, 지급 재처리, 감사 로그)</summary>
    public const string RewardManager = nameof(RewardManager);

    public static IServiceCollection AddAdminAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(AnyStaff, p => p.RequireRole(AdminRoles.Admin, AdminRoles.Operator, AdminRoles.CustomerSupport))
            .AddPolicy(ContentEditor, p => p.RequireRole(AdminRoles.Admin, AdminRoles.Operator))
            .AddPolicy(RewardManager, p => p.RequireRole(AdminRoles.Admin))
            // 정책을 명시하지 않은 엔드포인트도 기본적으로 직원 권한을 요구 (실수로 열리는 API 방지)
            .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireRole(AdminRoles.Admin, AdminRoles.Operator, AdminRoles.CustomerSupport)
                .Build());
        return services;
    }
}
