using GamePortal.AspNetCore;
using GamePortal.Web.Api.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddPortalApiDefaults("GamePortal Web API");
builder.Services.AddPortalRateLimiting(builder.Configuration);

var app = builder.Build();

app.UsePortalApiDefaults();
app.UseRateLimiter();

app.Run();

/// <summary>WebApplicationFactory 에서 참조하기 위한 선언.</summary>
public partial class Program;
