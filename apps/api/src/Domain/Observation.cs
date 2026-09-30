namespace Triggered.Domain;

public sealed class Observation
{
    private Observation()
    {
    }

    public Observation(Food food, DateTimeOffset consumedAt, string reaction)
    {
        ArgumentNullException.ThrowIfNull(food);

        if (consumedAt == default)
        {
            throw new ArgumentException("Consumption time is required.", nameof(consumedAt));
        }

        if (string.IsNullOrWhiteSpace(reaction))
        {
            throw new ArgumentException("Reaction is required.", nameof(reaction));
        }

        Id = Guid.NewGuid();
        FoodId = food.Id;
        Food = food;
        ConsumedAt = consumedAt;
        Reaction = reaction;
    }

    public Guid Id { get; private set; }

    public Guid FoodId { get; private set; }

    public Food Food { get; private set; } = null!;

    public DateTimeOffset ConsumedAt { get; private set; }

    public string Reaction { get; private set; } = null!;
}
