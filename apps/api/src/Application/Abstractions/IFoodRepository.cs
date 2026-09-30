using Triggered.Domain;

namespace Triggered.Application.Abstractions;

public interface IFoodRepository
{
    Task<Food?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
