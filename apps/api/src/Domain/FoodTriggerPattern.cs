namespace Triggered.Domain;

public enum FoodTriggerPatternLevel
{
    InsufficientData,
    Low,
    PossibleTrigger,
    Moderate,
    LikelyTrigger,
    StrongTriggerPattern
}

public sealed record FoodTriggerPattern(
    FoodTriggerPatternLevel Level,
    int ObservationCount,
    int ReportedReactionCount);
