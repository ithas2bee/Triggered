using Microsoft.EntityFrameworkCore;
using Triggered.Application.Abstractions;
using Triggered.Domain;

namespace Triggered.Infrastructure.Persistence.Repositories;

public sealed class ObservationRepository(TriggeredDbContext context) : IObservationRepository
{
    public async Task<IReadOnlyList<Observation>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var observations = await context.Observations.ToListAsync(cancellationToken);
        return observations.OrderByDescending(observation => observation.ConsumedAt).ToArray();
    }

    public async Task AddAsync(Observation observation, CancellationToken cancellationToken = default)
    {
        await context.Observations.AddAsync(observation, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
