using Triggered.Application.Abstractions;
using Triggered.Application.UseCases;
using Triggered.Domain;

namespace Foundation.Tests;

public sealed class GetFoodByIdUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_returns_food_matching_id_and_passes_cancellation_token()
    {
        var food = new Food("Ketchup");
        var foodRepository = new FakeFoodRepository(food);
        var useCase = new GetFoodByIdUseCase(foodRepository);
        using var cancellationSource = new CancellationTokenSource();

        var result = await useCase.ExecuteAsync(food.Id, cancellationSource.Token);

        Assert.Same(food, result);
        Assert.Equal(food.Id, result!.Id);
        Assert.Equal("Ketchup", result.Name);
        Assert.Equal(cancellationSource.Token, foodRepository.CancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_returns_null_when_food_does_not_exist()
    {
        var useCase = new GetFoodByIdUseCase(new FakeFoodRepository(new Food("Ketchup")));

        var result = await useCase.ExecuteAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    private sealed class FakeFoodRepository(Food food) : IFoodRepository
    {
        public CancellationToken CancellationToken { get; private set; }

        public Task<Food?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            return Task.FromResult(id == food.Id ? food : null);
        }

        public Task<IReadOnlyList<Food>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Food>>([food]);

        public Task AddAsync(Food food, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
