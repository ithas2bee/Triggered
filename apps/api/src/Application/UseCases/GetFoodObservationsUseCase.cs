using Triggered.Application.Abstractions;
using Triggered.Domain;

namespace Triggered.Application.UseCases;

public sealed class GetFoodObservationsUseCase(IObservationRepository observationRepository)
{
    public Task<IReadOnlyList<Observation>> ExecuteAsync(
        Guid foodId,
        CancellationToken cancellationToken = default) =>
        observationRepository.GetByFoodIdAsync(foodId, cancellationToken);
}
