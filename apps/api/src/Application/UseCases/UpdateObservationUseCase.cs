using Triggered.Application.Abstractions;
using Triggered.Domain;

namespace Triggered.Application.UseCases;

public sealed class UpdateObservationUseCase(IObservationRepository observationRepository)
{
    public async Task<Observation?> ExecuteAsync(
        Guid observationId,
        DateTimeOffset consumedAt,
        string? reaction,
        CancellationToken cancellationToken = default)
    {
        var observation = await observationRepository.GetByIdAsync(observationId, cancellationToken);
        if (observation is null)
        {
            return null;
        }

        observation.Update(consumedAt, reaction);
        await observationRepository.UpdateAsync(observation, cancellationToken);

        return observation;
    }
}
