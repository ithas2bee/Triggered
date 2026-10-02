using Triggered.Application.Abstractions;
using Triggered.Application.UseCases;
using Triggered.Domain;

namespace Foundation.Tests;

public sealed class CreateFoodUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_creates_and_persists_food()
    {
        var foodRepository = new FakeFoodRepository();
        var useCase = new CreateFoodUseCase(foodRepository);
        using var cancellationSource = new CancellationTokenSource();

        var food = await useCase.ExecuteAsync("Ketchup", cancellationSource.Token);

        Assert.Equal("Ketchup", food.Name);
        Assert.NotEqual(Guid.Empty, food.Id);
        Assert.Same(food, Assert.Single(foodRepository.Foods));
        Assert.Equal(cancellationSource.Token, foodRepository.CancellationToken);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task ExecuteAsync_does_not_persist_invalid_food(string name)
    {
        var foodRepository = new FakeFoodRepository();
        var useCase = new CreateFoodUseCase(foodRepository);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(name));

        Assert.Empty(foodRepository.Foods);
    }

    private sealed class FakeFoodRepository : IFoodRepository
    {
        public List<Food> Foods { get; } = [];
        public CancellationToken CancellationToken { get; private set; }

        public Task<Food?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Foods.SingleOrDefault(food => food.Id == id));
        }

        public Task AddAsync(Food food, CancellationToken cancellationToken = default)
        {
            Foods.Add(food);
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }
}
