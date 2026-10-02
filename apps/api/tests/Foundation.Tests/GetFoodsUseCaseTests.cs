using Triggered.Application.Abstractions;
using Triggered.Application.UseCases;
using Triggered.Domain;

namespace Foundation.Tests;

public sealed class GetFoodsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_returns_all_foods_and_passes_cancellation_token()
    {
        var foods = new[] { new Food("Ketchup"), new Food("Pizza") };
        var foodRepository = new FakeFoodRepository(foods);
        var useCase = new GetFoodsUseCase(foodRepository);
        using var cancellationSource = new CancellationTokenSource();

        var result = await useCase.ExecuteAsync(cancellationSource.Token);

        Assert.Equal(foods.Select(food => food.Id), result.Select(food => food.Id));
        Assert.Equal(foods.Select(food => food.Name), result.Select(food => food.Name));
        Assert.Equal(cancellationSource.Token, foodRepository.CancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_returns_empty_collection_when_repository_has_no_foods()
    {
        var useCase = new GetFoodsUseCase(new FakeFoodRepository([]));

        var result = await useCase.ExecuteAsync();

        Assert.Empty(result);
    }

    private sealed class FakeFoodRepository(IReadOnlyList<Food> foods) : IFoodRepository
    {
        public CancellationToken CancellationToken { get; private set; }

        public Task<Food?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(foods.SingleOrDefault(food => food.Id == id));

        public Task<IReadOnlyList<Food>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            return Task.FromResult(foods);
        }

        public Task AddAsync(Food food, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
