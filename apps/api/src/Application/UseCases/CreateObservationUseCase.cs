using Triggered.Application.Abstractions;
using Triggered.Domain;

namespace Triggered.Application.UseCases;

public sealed class CreateObservationUseCase(
    IFoodRepository foodRepository,
    IObservationRepository observationRepository)
{
    public async Task<Observation?> ExecuteAsync(
        Guid foodId,
        DateTimeOffset consumedAt,
        string reaction,
        CancellationToken cancellationToken = default)
    {
        var food = await foodRepository.GetByIdAsync(foodId, cancellationToken);
        if (food is null)
        {
            return null;
        }

        var observation = new Observation(food, consumedAt, reaction);
        await observationRepository.AddAsync(observation, cancellationToken);

        return observation;
    }
}
