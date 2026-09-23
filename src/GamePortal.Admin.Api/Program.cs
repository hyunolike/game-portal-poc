using GamePortal.Admin.Api.Authorization;
using GamePortal.AspNetCore;
using GamePortal.Infrastructure;
using GamePortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddPortalApiDefaults("GamePortal Admin API");
builder.Services.AddAuditing();
builder.Services.AddAdminAuthorization();

// 운영툴 프론트엔드(사내망) 도메인만 허용
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// 로컬 개발 편의용. 운영 배포에서는 CD 파이프라인의 EF 마이그레이션 번들로 적용한다.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<PortalDbContext>().Database.MigrateAsync();
}

app.UseCors();
app.UsePortalApiDefaults();

await app.RunAsync();

/// <summary>WebApplicationFactory 에서 참조하기 위한 선언.</summary>
public partial class Program;
