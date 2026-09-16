using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Triggered.Infrastructure;
using Triggered.Infrastructure.Persistence;

namespace Foundation.Tests;

public sealed class InfrastructureTests
{
    [Fact]
    public void AddInfrastructure_registers_sql_server_db_context()
    {
        var configuration = new ConfigurationManager();
        configuration["ConnectionStrings:TriggeredDatabase"] = "Server=localhost;Database=Triggered";
        using var services = new ServiceCollection()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();

        using var context = services.GetRequiredService<TriggeredDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
    }

    [Fact]
    public void AddInfrastructure_requires_database_connection_string()
    {
        var configuration = new ConfigurationManager();

        var exception = Assert.Throws<InvalidOperationException>(
            () =>
            {
                new ServiceCollection().AddInfrastructure(configuration);
            });

        Assert.Contains("TriggeredDatabase", exception.Message);
    }
}
