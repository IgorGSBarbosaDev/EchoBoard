using Microsoft.EntityFrameworkCore;

namespace EchoBoard.Infrastructure.Persistence;

public sealed class EchoBoardDbContext : DbContext
{
    public EchoBoardDbContext(DbContextOptions<EchoBoardDbContext> options)
        : base(options)
    {
    }
}
