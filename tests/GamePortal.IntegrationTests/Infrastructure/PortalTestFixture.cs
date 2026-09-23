extern alias AdminApi;
extern alias WebApi;

using GamePortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Testcontainers.MsSql;

namespace GamePortal.IntegrationTests.Infrastructure;

/// <summary>
/// 실제 SQL Server 컨테이너 1개 + Web/Admin API 인스턴스.
/// 동시성(조건부 UPDATE, UNIQUE 인덱스, 락 힌트)은 InMemory/SQLite 로는 검증할 수 없으므로 실제 DB 를 쓴다.
/// </summary>
public sealed class PortalTestFixture : IAsyncLifetime
{
    public const string WebSigningKey = "integration-test-web-signing-key-0123456789";
    public const string AdminSigningKey = "integration-test-admin-signing-key-0123456789";

    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    // 운영의 Redis 처럼 Web/Admin 두 프로세스가 같은 캐시를 보도록 공유 인스턴스를 주입
    private readonly IDistributedCache _sharedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    public string ConnectionString { get; private set; } = string.Empty;

    public WebApplicationFactory<WebApi::Program> Web { get; private set; } = null!;

    public WebApplicationFactory<AdminApi::Program> Admin { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();
        ConnectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            InitialCatalog = "GamePortalTest",
        }.ConnectionString;

        await using (var db = CreateDbContext())
        {
            await db.Database.MigrateAsync();
        }

        Web = new WebApplicationFactory<WebApi::Program>().WithWebHostBuilder(b => Configure(b, WebSigningKey));
        Admin = new WebApplicationFactory<AdminApi::Program>().WithWebHostBuilder(b => Configure(b, AdminSigningKey));
    }

    public PortalDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task DisposeAsync()
    {
        if (Web is not null)
        {
            await Web.DisposeAsync();
        }

        if (Admin is not null)
        {
            await Admin.DisposeAsync();
        }

        await _sql.DisposeAsync();
    }

    private void Configure(IWebHostBuilder builder, string signingKey)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:PortalDb", ConnectionString);
        builder.UseSetting("ConnectionStrings:Redis", string.Empty);
        builder.UseSetting("Jwt:SigningKey", signingKey);
        builder.UseSetting("RateLimiting:PerIpBurst", "100000");
        builder.UseSetting("RateLimiting:PerIpPerSecond", "100000");
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDistributedCache>();
            services.AddSingleton(_sharedCache);
        });
    }
}

[CollectionDefinition(Name)]
public sealed class PortalCollection : ICollectionFixture<PortalTestFixture>
{
    public const string Name = "portal";
}
