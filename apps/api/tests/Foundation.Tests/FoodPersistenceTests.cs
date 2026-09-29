using Microsoft.EntityFrameworkCore;
using Triggered.Infrastructure.Persistence;

namespace Foundation.Tests;

public sealed class FoodPersistenceTests
{
    [Fact]
    public void Food_mapping_has_required_key_and_name()
    {
        var options = new DbContextOptionsBuilder<TriggeredDbContext>()
            .UseSqlServer("Server=(local);Database=Triggered")
            .Options;

        using var context = new TriggeredDbContext(options);
        var entity = context.Model.FindEntityType("Triggered.Domain.Food");

        Assert.NotNull(entity);
        Assert.Equal("Foods", entity.GetTableName());
        Assert.Equal("Id", entity.FindPrimaryKey()!.Properties.Single().Name);
        Assert.False(entity.FindProperty("Name")!.IsNullable);
    }
}
