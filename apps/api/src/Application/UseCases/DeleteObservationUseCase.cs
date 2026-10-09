using Triggered.Application.Abstractions;

namespace Triggered.Application.UseCases;

public sealed class DeleteObservationUseCase(IObservationRepository observationRepository)
{
    public async Task<bool> ExecuteAsync(Guid observationId, CancellationToken cancellationToken = default)
    {
        var observation = await observationRepository.GetByIdAsync(observationId, cancellationToken);
        if (observation is null)
        {
            return false;
        }

        await observationRepository.DeleteAsync(observation, cancellationToken);
        return true;
    }
}
