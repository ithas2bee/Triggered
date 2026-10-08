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

    [Fact]
    public void Update_changes_consumption_time_and_reaction_without_changing_identity_or_food()
    {
        var food = new Food("Ketchup");
        var originalConsumedAt = new DateTimeOffset(2026, 9, 29, 12, 30, 0, TimeSpan.Zero);
        var observation = new Observation(food, originalConsumedAt, "Heartburn");
        var updatedConsumedAt = originalConsumedAt.AddHours(1);

        observation.Update(updatedConsumedAt, "No reaction");

        Assert.Equal(food.Id, observation.FoodId);
        Assert.Same(food, observation.Food);
        Assert.Equal(updatedConsumedAt, observation.ConsumedAt);
        Assert.Equal("No reaction", observation.Reaction);
    }

    [Theory]
    [InlineData(true, "No reaction")]
    [InlineData(true, " ")]
    public void Update_rejects_invalid_data_without_changing_observation(
        bool missingConsumedAt,
        string reaction)
    {
        var originalConsumedAt = new DateTimeOffset(2026, 9, 29, 12, 30, 0, TimeSpan.Zero);
        var observation = new Observation(new Food("Ketchup"), originalConsumedAt, "Heartburn");

        Assert.Throws<ArgumentException>(() =>
            observation.Update(missingConsumedAt ? default : originalConsumedAt.AddHours(1), reaction));

        Assert.Equal(originalConsumedAt, observation.ConsumedAt);
        Assert.Equal("Heartburn", observation.Reaction);
    }

    [Fact]
    public void Update_rejects_missing_reaction()
    {
        var originalConsumedAt = new DateTimeOffset(2026, 9, 29, 12, 30, 0, TimeSpan.Zero);
        var observation = new Observation(new Food("Ketchup"), originalConsumedAt, "Heartburn");

        Assert.Throws<ArgumentException>(() =>
            observation.Update(originalConsumedAt.AddHours(1), null));

        Assert.Equal(originalConsumedAt, observation.ConsumedAt);
        Assert.Equal("Heartburn", observation.Reaction);
    }
}
