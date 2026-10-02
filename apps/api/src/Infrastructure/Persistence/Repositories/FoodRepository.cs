using Microsoft.EntityFrameworkCore;
using Triggered.Application.Abstractions;
using Triggered.Domain;

namespace Triggered.Infrastructure.Persistence.Repositories;

public sealed class FoodRepository(TriggeredDbContext context) : IFoodRepository
{
    public Task<Food?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Foods.FirstOrDefaultAsync(food => food.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Food>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Foods.ToListAsync(cancellationToken);

    public async Task AddAsync(Food food, CancellationToken cancellationToken = default)
    {
        await context.Foods.AddAsync(food, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
