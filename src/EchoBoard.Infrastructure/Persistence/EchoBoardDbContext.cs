using EchoBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EchoBoard.Infrastructure.Persistence;

public sealed class EchoBoardDbContext : DbContext
{
    public EchoBoardDbContext(DbContextOptions<EchoBoardDbContext> options)
        : base(options)
    {
    }

    public DbSet<Sound> Sounds => Set<Sound>();

    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EchoBoardDbContext).Assembly);
    }
}
