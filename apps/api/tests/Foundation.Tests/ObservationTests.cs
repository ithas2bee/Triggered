using Triggered.Domain;

namespace Foundation.Tests;

public sealed class ObservationTests
{
    [Fact]
    public void Constructor_creates_observation_for_food()
    {
        var food = new Food("Ketchup");
        var consumedAt = new DateTimeOffset(2026, 9, 29, 12, 30, 0, TimeSpan.Zero);

        var observation = new Observation(food, consumedAt, "Heartburn");

        Assert.NotEqual(Guid.Empty, observation.Id);
        Assert.Equal(food.Id, observation.FoodId);
        Assert.Same(food, observation.Food);
        Assert.Equal(consumedAt, observation.ConsumedAt);
        Assert.Equal("Heartburn", observation.Reaction);
    }

    [Fact]
    public void Constructor_rejects_missing_food()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Observation(null!, DateTimeOffset.UtcNow, "Heartburn"));
    }

    [Fact]
    public void Constructor_rejects_missing_consumption_time()
    {
        Assert.Throws<ArgumentException>(() =>
            new Observation(new Food("Ketchup"), default, "Heartburn"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Constructor_rejects_missing_reaction(string reaction)
    {
        Assert.Throws<ArgumentException>(() =>
            new Observation(new Food("Ketchup"), DateTimeOffset.UtcNow, reaction));
    }
}
