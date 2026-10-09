using Triggered.Domain;

namespace Triggered.Application.Abstractions;

public interface IObservationRepository
{
    Task<Observation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Observation>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Observation>> GetByFoodIdAsync(
        Guid foodId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Observation observation, CancellationToken cancellationToken = default);

    Task UpdateAsync(Observation observation, CancellationToken cancellationToken = default);

    Task DeleteAsync(Observation observation, CancellationToken cancellationToken = default);
}
