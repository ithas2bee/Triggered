using Microsoft.EntityFrameworkCore;
using Triggered.Domain;

namespace Triggered.Infrastructure.Persistence;

public sealed class TriggeredDbContext(DbContextOptions<TriggeredDbContext> options)
    : DbContext(options)
{
    public DbSet<Food> Foods => Set<Food>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TriggeredDbContext).Assembly);
    }
}
