using Triggered.Application.Abstractions;
using Triggered.Application.UseCases;
using Triggered.Domain;

namespace Foundation.Tests;

public sealed class GetObservationsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_returns_observations_and_passes_cancellation_token()
    {
        var food = new Food("Ketchup");
        var observations = new[]
        {
            new Observation(food, new DateTimeOffset(2026, 9, 29, 12, 30, 0, TimeSpan.Zero), "Heartburn"),
            new Observation(food, new DateTimeOffset(2026, 9, 30, 12, 30, 0, TimeSpan.Zero), "Bloating")
        };
        var repository = new FakeObservationRepository(observations);
        var useCase = new GetObservationsUseCase(repository);
        using var cancellationSource = new CancellationTokenSource();

        var result = await useCase.ExecuteAsync(cancellationSource.Token);

        Assert.Equal(observations, result);
        Assert.Equal(cancellationSource.Token, repository.CancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_returns_empty_collection_when_repository_has_no_observations()
    {
        var useCase = new GetObservationsUseCase(new FakeObservationRepository([]));

        var result = await useCase.ExecuteAsync();

        Assert.Empty(result);
    }

    private sealed class FakeObservationRepository(IReadOnlyList<Observation> observations)
        : IObservationRepository
    {
        public CancellationToken CancellationToken { get; private set; }

        public Task<Observation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(observations.SingleOrDefault(observation => observation.Id == id));

        public Task<IReadOnlyList<Observation>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            return Task.FromResult(observations);
        }

        public Task<IReadOnlyList<Observation>> GetByFoodIdAsync(
            Guid foodId,
            CancellationToken cancellationToken = default)
        {
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
