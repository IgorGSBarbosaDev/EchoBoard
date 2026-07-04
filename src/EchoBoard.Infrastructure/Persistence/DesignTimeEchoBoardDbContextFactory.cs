using EchoBoard.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EchoBoard.Infrastructure.Persistence;

public sealed class DesignTimeEchoBoardDbContextFactory : IDesignTimeDbContextFactory<EchoBoardDbContext>
{
    public EchoBoardDbContext CreateDbContext(string[] args)
    {
        var settings = AppSettings.CreateDefault();
        var options = new DbContextOptionsBuilder<EchoBoardDbContext>()
            .UseSqlite(settings.DatabaseConnectionString)
            .Options;

        return new EchoBoardDbContext(options);
    }
}
