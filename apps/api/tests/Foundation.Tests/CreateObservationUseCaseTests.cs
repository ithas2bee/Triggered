using Triggered.Application.Abstractions;
using Triggered.Application.UseCases;
using Triggered.Domain;

namespace Foundation.Tests;

public sealed class CreateObservationUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_creates_and_persists_observation_for_existing_food()
    {
        var food = new Food("Ketchup");
        var foodRepository = new FakeFoodRepository(food);
        var observationRepository = new FakeObservationRepository();
        var useCase = new CreateObservationUseCase(foodRepository, observationRepository);
        var consumedAt = new DateTimeOffset(2026, 9, 29, 12, 30, 0, TimeSpan.Zero);

        var observation = await useCase.ExecuteAsync(food.Id, consumedAt, "Heartburn");

        Assert.NotNull(observation);
        Assert.Equal(food.Id, observation.FoodId);
        Assert.Same(food, observation.Food);
        Assert.Equal(consumedAt, observation.ConsumedAt);
        Assert.Equal("Heartburn", observation.Reaction);
        Assert.Same(observation, Assert.Single(observationRepository.Observations));
    }

    [Fact]
    public async Task ExecuteAsync_returns_null_and_does_not_persist_when_food_does_not_exist()
    {
        var foodRepository = new FakeFoodRepository(null);
        var observationRepository = new FakeObservationRepository();
        var useCase = new CreateObservationUseCase(foodRepository, observationRepository);

        var observation = await useCase.ExecuteAsync(Guid.NewGuid(), DateTimeOffset.UtcNow, "Heartburn");

        Assert.Null(observation);
        Assert.Empty(observationRepository.Observations);
    }

    [Fact]
    public async Task ExecuteAsync_preserves_domain_validation_and_does_not_persist_invalid_observation()
    {
        var food = new Food("Ketchup");
        var foodRepository = new FakeFoodRepository(food);
        var observationRepository = new FakeObservationRepository();
        var useCase = new CreateObservationUseCase(foodRepository, observationRepository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(food.Id, default, "Heartburn"));

        Assert.Empty(observationRepository.Observations);
    }

    private sealed class FakeFoodRepository(Food? food) : IFoodRepository
    {
        public Task<Food?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(food?.Id == id ? food : null);
        }

        public Task<IReadOnlyList<Food>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Food>>([]);
        }

        public Task AddAsync(Food food, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeObservationRepository : IObservationRepository
    {
        public List<Observation> Observations { get; } = [];

        public Task<Observation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Observations.SingleOrDefault(observation => observation.Id == id));

        public Task<IReadOnlyList<Observation>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Observation>>(Observations);

        public Task<IReadOnlyList<Observation>> GetByFoodIdAsync(
            Guid foodId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Observation>>(
                Observations.Where(observation => observation.FoodId == foodId).ToArray());

        public Task AddAsync(Observation observation, CancellationToken cancellationToken = default)
        {
            Observations.Add(observation);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Observation observation, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
