using Triggered.Application.Abstractions;
using Triggered.Domain;

namespace Triggered.Application.UseCases;

public sealed class GetFoodByIdUseCase(IFoodRepository foodRepository)
{
    public Task<Food?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default) =>
        foodRepository.GetByIdAsync(id, cancellationToken);
}
