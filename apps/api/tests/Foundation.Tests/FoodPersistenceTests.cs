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

    [Fact]
    public void Observation_mapping_has_required_fields_and_food_relationship()
    {
        var options = new DbContextOptionsBuilder<TriggeredDbContext>()
            .UseSqlServer("Server=(local);Database=Triggered")
            .Options;

        using var context = new TriggeredDbContext(options);
        var entity = context.Model.FindEntityType("Triggered.Domain.Observation");

        Assert.NotNull(entity);
        Assert.Equal("Observations", entity.GetTableName());
        Assert.Equal("Id", entity.FindPrimaryKey()!.Properties.Single().Name);
        Assert.False(entity.FindProperty("FoodId")!.IsNullable);
        Assert.False(entity.FindProperty("ConsumedAt")!.IsNullable);
        Assert.False(entity.FindProperty("Reaction")!.IsNullable);

        var foreignKey = Assert.Single(entity.GetForeignKeys());
        Assert.Equal("Triggered.Domain.Food", foreignKey.PrincipalEntityType.Name);
        Assert.Equal("FoodId", foreignKey.Properties.Single().Name);
    }
}
