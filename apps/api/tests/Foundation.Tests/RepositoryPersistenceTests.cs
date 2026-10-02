using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Triggered.Domain;
using Triggered.Infrastructure.Persistence;
using Triggered.Infrastructure.Persistence.Repositories;

namespace Foundation.Tests;

public sealed class RepositoryPersistenceTests
{
    [Fact]
    public async Task FoodRepository_gets_all_foods_from_database()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TriggeredDbContext>()
            .UseSqlite(connection)
            .Options;
        var foods = new[] { new Food("Oatmeal"), new Food("Pizza") };

        await using (var seedContext = new TriggeredDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            await seedContext.Foods.AddRangeAsync(foods);
            await seedContext.SaveChangesAsync();
        }

        await using var context = new TriggeredDbContext(options);
        var result = await new FoodRepository(context).GetAllAsync();

        Assert.Equal(foods.Length, result.Count);
        foreach (var food in foods)
        {
            Assert.Contains(result, persistedFood =>
                persistedFood.Id == food.Id && persistedFood.Name == food.Name);
        }
    }

    [Fact]
    public async Task FoodRepository_gets_food_by_id_from_database()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TriggeredDbContext>()
            .UseSqlite(connection)
            .Options;
        var food = new Food("Oatmeal");

        await using (var seedContext = new TriggeredDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            await seedContext.Foods.AddAsync(food);
            await seedContext.SaveChangesAsync();
        }

        await using var context = new TriggeredDbContext(options);
        var repository = new FoodRepository(context);

        var result = await repository.GetByIdAsync(food.Id);
        var missingFood = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Equal(food.Id, result.Id);
        Assert.Equal(food.Name, result.Name);
        Assert.Null(missingFood);
    }

    [Fact]
    public async Task ObservationRepository_persists_observation_and_food_relationship()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TriggeredDbContext>()
            .UseSqlite(connection)
            .Options;
        var food = new Food("Oatmeal");

        await using (var seedContext = new TriggeredDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            await seedContext.Foods.AddAsync(food);
            await seedContext.SaveChangesAsync();
        }

        var consumedAt = new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
        await using var context = new TriggeredDbContext(options);
        var persistedFood = await new FoodRepository(context).GetByIdAsync(food.Id);
        Assert.NotNull(persistedFood);
        var observation = new Observation(persistedFood, consumedAt, "Felt bloated");
        await new ObservationRepository(context).AddAsync(observation);

        await using var verificationContext = new TriggeredDbContext(options);
        var persistedObservation = await verificationContext.Observations
            .Include(savedObservation => savedObservation.Food)
            .SingleAsync(savedObservation => savedObservation.Id == observation.Id);

        Assert.Equal(observation.Id, persistedObservation.Id);
        Assert.Equal(food.Id, persistedObservation.FoodId);
        Assert.Equal(food.Name, persistedObservation.Food.Name);
        Assert.Equal(consumedAt, persistedObservation.ConsumedAt);
        Assert.Equal(observation.Reaction, persistedObservation.Reaction);
    }
}
