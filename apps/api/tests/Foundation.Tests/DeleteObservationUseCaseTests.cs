using Triggered.Application.Abstractions;
using Triggered.Application.UseCases;
using Triggered.Domain;

namespace Foundation.Tests;

public sealed class DeleteObservationUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_deletes_existing_observation_and_preserves_other_observations()
    {
        var food = new Food("Ketchup");
        var observation = new Observation(
            food,
            new DateTimeOffset(2026, 9, 29, 12, 30, 0, TimeSpan.Zero),
            "Heartburn");
        var otherObservation = new Observation(
            food,
            new DateTimeOffset(2026, 9, 30, 12, 30, 0, TimeSpan.Zero),
            "Bloating");
        var repository = new FakeObservationRepository([observation, otherObservation]);
        var useCase = new DeleteObservationUseCase(repository);
        using var cancellationSource = new CancellationTokenSource();

        var deleted = await useCase.ExecuteAsync(observation.Id, cancellationSource.Token);

        Assert.True(deleted);
        Assert.Same(observation, Assert.Single(repository.DeletedObservations));
        Assert.Same(otherObservation, Assert.Single(repository.Observations));
        Assert.Equal(cancellationSource.Token, repository.CancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_returns_false_and_does_not_delete_when_observation_does_not_exist()
    {
        var repository = new FakeObservationRepository([]);
        var useCase = new DeleteObservationUseCase(repository);

        var deleted = await useCase.ExecuteAsync(Guid.NewGuid());

        Assert.False(deleted);
        Assert.Empty(repository.DeletedObservations);
    }

    private sealed class FakeObservationRepository(IReadOnlyList<Observation> observations)
        : IObservationRepository
    {
        public List<Observation> Observations { get; } = [.. observations];

        public List<Observation> DeletedObservations { get; } = [];

        public CancellationToken CancellationToken { get; private set; }

        public Task<Observation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Observations.SingleOrDefault(observation => observation.Id == id));

        public Task<IReadOnlyList<Observation>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Observation>>(Observations);

        public Task<IReadOnlyList<Observation>> GetByFoodIdAsync(
            Guid foodId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Observation>>(
                Observations.Where(observation => observation.FoodId == foodId).ToArray());

        public Task AddAsync(Observation observation, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(Observation observation, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(Observation observation, CancellationToken cancellationToken = default)
        {
            DeletedObservations.Add(observation);
            Observations.Remove(observation);
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }
}
