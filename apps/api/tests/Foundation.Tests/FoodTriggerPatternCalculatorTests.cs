using Triggered.Domain;

namespace Foundation.Tests;

public sealed class FoodTriggerPatternCalculatorTests
{
    [Theory]
    [InlineData(0, FoodTriggerPatternLevel.InsufficientData)]
    [InlineData(2, FoodTriggerPatternLevel.InsufficientData)]
    public void Calculate_returns_insufficient_data_below_minimum_observation_count(
        int observationCount,
        FoodTriggerPatternLevel expectedLevel)
    {
        var food = new Food("Ketchup");
        var observations = CreateObservations(food, observationCount, reportedReactionCount: observationCount);

        var result = FoodTriggerPatternCalculator.Calculate(food.Id, observations);

        Assert.Equal(expectedLevel, result.Level);
        Assert.Equal(observationCount, result.ObservationCount);
    }

    [Theory]
    [InlineData(0, FoodTriggerPatternLevel.Low)]
    [InlineData(1, FoodTriggerPatternLevel.Low)]
    [InlineData(2, FoodTriggerPatternLevel.PossibleTrigger)]
    [InlineData(3, FoodTriggerPatternLevel.Moderate)]
    [InlineData(4, FoodTriggerPatternLevel.LikelyTrigger)]
    [InlineData(5, FoodTriggerPatternLevel.StrongTriggerPattern)]
    public void Calculate_maps_reported_reaction_percentage_to_pattern_level(
        int reportedReactionCount,
        FoodTriggerPatternLevel expectedLevel)
    {
        var food = new Food("Ketchup");
        var observations = CreateObservations(food, observationCount: 5, reportedReactionCount);

        var result = FoodTriggerPatternCalculator.Calculate(food.Id, observations);

        Assert.Equal(expectedLevel, result.Level);
        Assert.Equal(5, result.ObservationCount);
        Assert.Equal(reportedReactionCount, result.ReportedReactionCount);
    }

    [Fact]
    public void Calculate_counts_only_observations_for_requested_food()
    {
        var requestedFood = new Food("Ketchup");
        var otherFood = new Food("Mustard");
        var observations = CreateObservations(requestedFood, observationCount: 3, reportedReactionCount: 3)
            .Concat(CreateObservations(otherFood, observationCount: 10, reportedReactionCount: 0));

        var result = FoodTriggerPatternCalculator.Calculate(requestedFood.Id, observations);

        Assert.Equal(FoodTriggerPatternLevel.StrongTriggerPattern, result.Level);
        Assert.Equal(3, result.ObservationCount);
        Assert.Equal(3, result.ReportedReactionCount);
    }

    [Fact]
    public void Calculate_treats_explicit_no_reaction_as_no_reported_reaction()
    {
        var food = new Food("Ketchup");
        var observations = CreateObservations(food, observationCount: 3, reportedReactionCount: 0);

        var result = FoodTriggerPatternCalculator.Calculate(
            food.Id,
            [.. observations, new Observation(food, DateTimeOffset.UtcNow, " no REACTION ")]);

        Assert.Equal(FoodTriggerPatternLevel.Low, result.Level);
        Assert.Equal(4, result.ObservationCount);
        Assert.Equal(0, result.ReportedReactionCount);
    }

    [Fact]
    public void Calculate_returns_same_result_for_same_observations()
    {
        var food = new Food("Ketchup");
        var observations = CreateObservations(food, observationCount: 4, reportedReactionCount: 3);

        var firstResult = FoodTriggerPatternCalculator.Calculate(food.Id, observations);
        var secondResult = FoodTriggerPatternCalculator.Calculate(food.Id, observations);

        Assert.Equal(firstResult, secondResult);
    }

    private static Observation[] CreateObservations(
        Food food,
        int observationCount,
        int reportedReactionCount)
    {
        var consumedAt = new DateTimeOffset(2026, 9, 29, 12, 30, 0, TimeSpan.Zero);
        return Enumerable.Range(0, observationCount)
            .Select(index => new Observation(
                food,
                consumedAt.AddDays(index),
                index < reportedReactionCount ? "Heartburn" : "No reaction"))
            .ToArray();
    }
}
