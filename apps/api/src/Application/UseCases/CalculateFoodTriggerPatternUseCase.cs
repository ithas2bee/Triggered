using Triggered.Application.Abstractions;
using Triggered.Domain;

namespace Triggered.Application.UseCases;

public sealed class CalculateFoodTriggerPatternUseCase(IObservationRepository observationRepository)
{
    public async Task<FoodTriggerPattern> ExecuteAsync(
        Guid foodId,
        CancellationToken cancellationToken = default)
    {
        var observations = await observationRepository.GetByFoodIdAsync(foodId, cancellationToken);
        return FoodTriggerPatternCalculator.Calculate(foodId, observations);
    }
}
