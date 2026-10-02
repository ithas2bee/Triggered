using Triggered.Domain;

namespace Triggered.Application.Abstractions;

public interface IFoodRepository
{
    Task<Food?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Food>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Food food, CancellationToken cancellationToken = default);
}
