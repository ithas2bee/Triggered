using Triggered.Domain;

namespace Foundation.Tests;

public sealed class FoodTests
{
    [Fact]
    public void Constructor_creates_food_with_stable_identity_and_trimmed_name()
    {
        var food = new Food("  Apple  ");

        Assert.NotEqual(Guid.Empty, food.Id);
        Assert.Equal("Apple", food.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Constructor_rejects_missing_name(string name)
    {
        Assert.Throws<ArgumentException>(() => new Food(name));
    }
}
