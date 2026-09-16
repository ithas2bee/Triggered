namespace Triggered.Domain;

public sealed class Food
{
    private Food()
    {
    }

    public Food(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Food name is required.", nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;
}
