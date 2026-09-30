using Triggered.Application.Abstractions;
using Triggered.Domain;

namespace Triggered.Infrastructure.Persistence.Repositories;

public sealed class ObservationRepository(TriggeredDbContext context) : IObservationRepository
{
    public async Task AddAsync(Observation observation, CancellationToken cancellationToken = default)
    {
        await context.Observations.AddAsync(observation, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
