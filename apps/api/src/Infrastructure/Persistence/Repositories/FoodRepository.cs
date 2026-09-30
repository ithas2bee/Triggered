using Microsoft.EntityFrameworkCore;
using Triggered.Application.Abstractions;
using Triggered.Domain;

namespace Triggered.Infrastructure.Persistence.Repositories;

public sealed class FoodRepository(TriggeredDbContext context) : IFoodRepository
{
    public Task<Food?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Foods.FirstOrDefaultAsync(food => food.Id == id, cancellationToken);
}
