namespace Triggered.Domain;

public static class FoodTriggerPatternCalculator
{
    private const string NoReaction = "No reaction";
    private const int MinimumObservationCount = 3;

    public static FoodTriggerPattern Calculate(
        Guid foodId,
        IEnumerable<Observation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);

        var foodObservations = observations
            .Where(observation => observation.FoodId == foodId)
            .ToArray();
        var observationCount = foodObservations.Length;
        var reportedReactionCount = foodObservations.Count(observation =>
            !string.Equals(
                observation.Reaction.Trim(),
                NoReaction,
                StringComparison.OrdinalIgnoreCase));

        var level = observationCount < MinimumObservationCount
            ? FoodTriggerPatternLevel.InsufficientData
            : CalculateLevel(observationCount, reportedReactionCount);

        return new FoodTriggerPattern(level, observationCount, reportedReactionCount);
    }

    private static FoodTriggerPatternLevel CalculateLevel(
        int observationCount,
        int reportedReactionCount)
    {
        var scaledReportedReactionCount = (long)reportedReactionCount * 5;
        var scaledObservationCount = (long)observationCount;

        if (scaledReportedReactionCount <= scaledObservationCount)
        {
            return FoodTriggerPatternLevel.Low;
        }

        if (scaledReportedReactionCount <= scaledObservationCount * 2)
        {
            return FoodTriggerPatternLevel.PossibleTrigger;
        }

        if (scaledReportedReactionCount <= scaledObservationCount * 3)
        {
            return FoodTriggerPatternLevel.Moderate;
        }

        if (scaledReportedReactionCount <= scaledObservationCount * 4)
        {
            return FoodTriggerPatternLevel.LikelyTrigger;
        }

        return FoodTriggerPatternLevel.StrongTriggerPattern;
    }
}
