using Triggered.Application.Abstractions;
using Triggered.Domain;

namespace Triggered.Application.UseCases;

public sealed class GetObservationsUseCase(IObservationRepository observationRepository)
{
    public Task<IReadOnlyList<Observation>> ExecuteAsync(CancellationToken cancellationToken = default) =>
        observationRepository.GetAllAsync(cancellationToken);
}
