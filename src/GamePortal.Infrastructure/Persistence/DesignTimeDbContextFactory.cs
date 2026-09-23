using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GamePortal.Infrastructure.Persistence;

/// <summary>
/// dotnet ef migrations 용. 실제 DB 에 접속하지 않으므로 더미 연결 문자열이어도 된다.
/// 사용: dotnet ef migrations add &lt;Name&gt; -p src/GamePortal.Infrastructure -o Persistence/Migrations
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PortalDbContext>
{
    public PortalDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PortalDb")
                               ?? "Server=localhost,1433;Database=GamePortal;User Id=sa;Password=Local_Dev_Passw0rd;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<PortalDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new PortalDbContext(options);
    }
}
