using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Triggered.Application.Abstractions;
using Triggered.Infrastructure.Persistence;
using Triggered.Infrastructure.Persistence.Repositories;

namespace Triggered.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public const string DatabaseConnectionName = "TriggeredDatabase";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DatabaseConnectionName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{DatabaseConnectionName}' is not configured.");
        }

        services.AddDbContext<TriggeredDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<IFoodRepository, FoodRepository>();
        services.AddScoped<IObservationRepository, ObservationRepository>();

        return services;
    }
}
