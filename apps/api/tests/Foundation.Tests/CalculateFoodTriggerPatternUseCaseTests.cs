using Triggered.Application.Abstractions;
using Triggered.Application.UseCases;
using Triggered.Domain;

namespace Foundation.Tests;

public sealed class CalculateFoodTriggerPatternUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_requests_observations_for_food_and_passes_cancellation_token()
    {
        var food = new Food("Ketchup");
        var observations = Enumerable.Range(0, 4)
            .Select(index => new Observation(
                food,
                new DateTimeOffset(2026, 9, 29, 12, 30, 0, TimeSpan.Zero).AddDays(index),
                index < 3 ? "Heartburn" : "No reaction"))
            .ToArray();
        var repository = new FakeObservationRepository(observations);
        var useCase = new CalculateFoodTriggerPatternUseCase(repository);
        using var cancellationSource = new CancellationTokenSource();

        var result = await useCase.ExecuteAsync(food.Id, cancellationSource.Token);

        Assert.Equal(FoodTriggerPatternLevel.LikelyTrigger, result.Level);
        Assert.Equal(food.Id, repository.RequestedFoodId);
        Assert.Equal(cancellationSource.Token, repository.CancellationToken);
    }

    private sealed class FakeObservationRepository(IReadOnlyList<Observation> observations)
        : IObservationRepository
    {
        public Guid RequestedFoodId { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<Observation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(observations.SingleOrDefault(observation => observation.Id == id));

        public Task<IReadOnlyList<Observation>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(observations);

        public Task<IReadOnlyList<Observation>> GetByFoodIdAsync(
            Guid foodId,
            CancellationToken cancellationToken = default)
        {
            RequestedFoodId = foodId;
            CancellationToken = cancellationToken;
            return Task.FromResult<IReadOnlyList<Observation>>(
                observations.Where(observation => observation.FoodId == foodId).ToArray());
        }

        public Task AddAsync(Observation observation, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(Observation observation, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(Observation observation, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
