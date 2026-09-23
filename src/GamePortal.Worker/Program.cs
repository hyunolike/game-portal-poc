using GamePortal.Infrastructure;
using Serilog;
using Serilog.Formatting.Compact;

// Outbox Worker: 웹 트랜잭션에서 기록된 지급 요청을 게임 서버로 전달한다.
// API 와 분리한 이유: (1) 게임 서버 장애가 웹 응답 지연으로 전파되지 않게 (2) 독립 스케일링/배포
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "GamePortal Worker")
    .WriteTo.Console(new RenderedCompactJsonFormatter()));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOutboxDispatcher(builder.Configuration);

await builder.Build().RunAsync();
