using Triggered.Application.Abstractions;
using Triggered.Application.UseCases;
using Triggered.Domain;

namespace Foundation.Tests;

public sealed class GetFoodObservationsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_returns_observations_for_food_and_passes_cancellation_token()
    {
        var foodId = Guid.NewGuid();
        var observations = new[]
        {
            new Observation(
                new Food("Ketchup"),
                new DateTimeOffset(2026, 9, 29, 12, 30, 0, TimeSpan.Zero),
                "Heartburn")
        };
        var repository = new FakeObservationRepository(observations);
        var useCase = new GetFoodObservationsUseCase(repository);
        using var cancellationSource = new CancellationTokenSource();

        var result = await useCase.ExecuteAsync(foodId, cancellationSource.Token);

        Assert.Equal(observations, result);
        Assert.Equal(foodId, repository.FoodId);
        Assert.Equal(cancellationSource.Token, repository.CancellationToken);
    }

    private sealed class FakeObservationRepository(IReadOnlyList<Observation> observations)
        : IObservationRepository
    {
        public Task<Observation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(observations.SingleOrDefault(observation => observation.Id == id));

        public Guid FoodId { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<IReadOnlyList<Observation>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(observations);

        public Task<IReadOnlyList<Observation>> GetByFoodIdAsync(
            Guid foodId,
            CancellationToken cancellationToken = default)
        {
            FoodId = foodId;
            CancellationToken = cancellationToken;
            return Task.FromResult(observations);
        }

        public Task AddAsync(Observation observation, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(Observation observation, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
