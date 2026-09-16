using Microsoft.EntityFrameworkCore;

namespace Triggered.Infrastructure.Persistence;

public sealed class TriggeredDbContext(DbContextOptions<TriggeredDbContext> options)
    : DbContext(options);
