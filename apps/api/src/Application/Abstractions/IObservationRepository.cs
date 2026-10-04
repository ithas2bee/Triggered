using Triggered.Domain;

namespace Triggered.Application.Abstractions;

public interface IObservationRepository
{
    Task<IReadOnlyList<Observation>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Observation>> GetByFoodIdAsync(
        Guid foodId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Observation observation, CancellationToken cancellationToken = default);
}
