using Triggered.Domain;

namespace Triggered.Application.Abstractions;

public interface IObservationRepository
{
    Task AddAsync(Observation observation, CancellationToken cancellationToken = default);
}
