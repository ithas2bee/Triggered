using Triggered.Application.Abstractions;
using Triggered.Application.UseCases;
using Triggered.Domain;

namespace Foundation.Tests;

public sealed class UpdateObservationUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_updates_existing_observation_without_creating_another()
    {
        var food = new Food("Ketchup");
        var originalConsumedAt = new DateTimeOffset(2026, 9, 29, 12, 30, 0, TimeSpan.Zero);
        var observation = new Observation(food, originalConsumedAt, "Heartburn");
        var repository = new FakeObservationRepository([observation]);
        var useCase = new UpdateObservationUseCase(repository);
        var updatedConsumedAt = originalConsumedAt.AddHours(1);
        using var cancellationSource = new CancellationTokenSource();

        var result = await useCase.ExecuteAsync(
            observation.Id,
            updatedConsumedAt,
            "No reaction",
            cancellationSource.Token);

        Assert.Same(observation, result);
        Assert.NotNull(result);
        Assert.Equal(food.Id, result.FoodId);
        Assert.Equal(updatedConsumedAt, result.ConsumedAt);
        Assert.Equal("No reaction", result.Reaction);
        Assert.Same(observation, Assert.Single(repository.Observations));
        Assert.Equal(1, repository.UpdateCount);
        Assert.Equal(cancellationSource.Token, repository.CancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_returns_null_when_observation_does_not_exist()
    {
        var repository = new FakeObservationRepository([]);
        var useCase = new UpdateObservationUseCase(repository);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), DateTimeOffset.UtcNow, "Heartburn");

        Assert.Null(result);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Theory]
    [InlineData(true, "No reaction")]
    [InlineData(false, " ")]
    public async Task ExecuteAsync_enforces_domain_validation(
        bool missingConsumedAt,
        string reaction)
    {
        var observation = new Observation(
            new Food("Ketchup"),
            new DateTimeOffset(2026, 9, 29, 12, 30, 0, TimeSpan.Zero),
            "Heartburn");
        var repository = new FakeObservationRepository([observation]);
        var useCase = new UpdateObservationUseCase(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(
                observation.Id,
                missingConsumedAt ? default : DateTimeOffset.UtcNow,
                reaction));

        Assert.Equal(0, repository.UpdateCount);
        Assert.Equal("Heartburn", observation.Reaction);
    }

    private sealed class FakeObservationRepository(IReadOnlyList<Observation> observations)
        : IObservationRepository
    {
        public IReadOnlyList<Observation> Observations => observations;

        public int UpdateCount { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<Observation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(observations.SingleOrDefault(observation => observation.Id == id));

        public Task<IReadOnlyList<Observation>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(observations);

        public Task<IReadOnlyList<Observation>> GetByFoodIdAsync(
            Guid foodId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Observation>>(
                observations.Where(observation => observation.FoodId == foodId).ToArray());

        public Task AddAsync(Observation observation, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(Observation observation, CancellationToken cancellationToken = default)
        {
            UpdateCount++;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }
}
