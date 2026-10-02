using Triggered.Application.Abstractions;
using Triggered.Domain;

namespace Triggered.Application.UseCases;

public sealed class GetFoodsUseCase(IFoodRepository foodRepository)
{
    public Task<IReadOnlyList<Food>> ExecuteAsync(CancellationToken cancellationToken = default) =>
        foodRepository.GetAllAsync(cancellationToken);
}
