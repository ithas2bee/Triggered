using Microsoft.EntityFrameworkCore;
using Triggered.Application.Abstractions;
using Triggered.Domain;

namespace Triggered.Infrastructure.Persistence.Repositories;

public sealed class ObservationRepository(TriggeredDbContext context) : IObservationRepository
{
    public Task<Observation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.Observations.SingleOrDefaultAsync(
            observation => observation.Id == id,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Observation>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var observations = await context.Observations.ToListAsync(cancellationToken);
        return observations.OrderByDescending(observation => observation.ConsumedAt).ToArray();
    }

    public async Task<IReadOnlyList<Observation>> GetByFoodIdAsync(
        Guid foodId,
        CancellationToken cancellationToken = default)
    {
        var observations = await context.Observations
            .Where(observation => observation.FoodId == foodId)
            .ToListAsync(cancellationToken);
        return observations.OrderByDescending(observation => observation.ConsumedAt).ToArray();
    }

    public async Task AddAsync(Observation observation, CancellationToken cancellationToken = default)
    {
        await context.Observations.AddAsync(observation, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Observation observation, CancellationToken cancellationToken = default)
    {
        context.Entry(observation).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Observation observation, CancellationToken cancellationToken = default)
    {
        context.Observations.Remove(observation);
        await context.SaveChangesAsync(cancellationToken);
    }
}
