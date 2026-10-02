using Triggered.Application.Abstractions;
using Triggered.Domain;

namespace Triggered.Application.UseCases;

public sealed class CreateFoodUseCase(IFoodRepository foodRepository)
{
    public async Task<Food> ExecuteAsync(string name, CancellationToken cancellationToken = default)
    {
        var food = new Food(name);
        await foodRepository.AddAsync(food, cancellationToken);

        return food;
    }
}
