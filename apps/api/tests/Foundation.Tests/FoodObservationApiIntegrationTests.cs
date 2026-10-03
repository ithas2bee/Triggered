using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Triggered.Application.Abstractions;
using Triggered.Application.UseCases;
using Triggered.Domain;
using Triggered.Infrastructure.Persistence;
using Triggered.Infrastructure.Persistence.Repositories;
using Triggered.Presentation;

namespace Foundation.Tests;

public sealed class FoodObservationApiIntegrationTests
{
    [Fact]
    public async Task Created_food_can_be_used_to_create_and_persist_observation_through_api()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var app = await CreateApiAsync(connection);

        await app.StartAsync();
        try
        {
            var addresses = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!;
            using var client = new HttpClient
            {
                BaseAddress = new Uri(addresses.Addresses.Single())
            };

            var foodResponse = await client.PostAsJsonAsync(
                "/api/foods",
                new CreateFoodRequest("Ketchup"));

            Assert.Equal(HttpStatusCode.Created, foodResponse.StatusCode);
            var createdFood = await foodResponse.Content.ReadFromJsonAsync<CreatedFoodResponse>();
            Assert.NotNull(createdFood);
            Assert.NotEqual(Guid.Empty, createdFood.Id);

            var consumedAt = new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
            const string reaction = "Heartburn";
            var observationResponse = await client.PostAsJsonAsync(
                "/api/observations",
                new CreateObservationRequest(createdFood.Id, consumedAt, reaction));

            Assert.Equal(HttpStatusCode.Created, observationResponse.StatusCode);
            var createdObservation =
                await observationResponse.Content.ReadFromJsonAsync<CreatedObservationResponse>();
            Assert.NotNull(createdObservation);
            Assert.NotEqual(Guid.Empty, createdObservation.Id);
            Assert.Equal(createdFood.Id, createdObservation.FoodId);
            Assert.Equal(consumedAt, createdObservation.ConsumedAt);
            Assert.Equal(reaction, createdObservation.Reaction);

            var laterConsumedAt = consumedAt.AddHours(1);
            var secondObservationResponse = await client.PostAsJsonAsync(
                "/api/observations",
                new CreateObservationRequest(createdFood.Id, laterConsumedAt, "Bloating"));
            Assert.Equal(HttpStatusCode.Created, secondObservationResponse.StatusCode);

            var observationsResponse = await client.GetAsync("/api/observations");
            Assert.Equal(HttpStatusCode.OK, observationsResponse.StatusCode);
            var returnedObservations =
                await observationsResponse.Content.ReadFromJsonAsync<ObservationResponse[]>();
            Assert.NotNull(returnedObservations);
            Assert.Equal(2, returnedObservations.Length);
            Assert.Equal(laterConsumedAt, returnedObservations[0].ConsumedAt);
            Assert.Equal(createdFood.Id, returnedObservations[0].FoodId);
            Assert.Equal("Bloating", returnedObservations[0].Reaction);
            Assert.Equal(consumedAt, returnedObservations[1].ConsumedAt);
            Assert.Equal(createdFood.Id, returnedObservations[1].FoodId);
            Assert.Equal(reaction, returnedObservations[1].Reaction);

            var missingFoodResponse = await client.PostAsJsonAsync(
                "/api/observations",
                new CreateObservationRequest(Guid.NewGuid(), consumedAt, reaction));

            Assert.Equal(HttpStatusCode.NotFound, missingFoodResponse.StatusCode);

            var secondFoodResponse = await client.PostAsJsonAsync(
                "/api/foods",
                new CreateFoodRequest("Pizza"));
            Assert.Equal(HttpStatusCode.Created, secondFoodResponse.StatusCode);
            var secondFood = await secondFoodResponse.Content.ReadFromJsonAsync<CreatedFoodResponse>();
            Assert.NotNull(secondFood);

            var foodsResponse = await client.GetAsync("/api/foods");
            Assert.Equal(HttpStatusCode.OK, foodsResponse.StatusCode);
            var returnedFoods = await foodsResponse.Content.ReadFromJsonAsync<FoodResponse[]>();
            Assert.NotNull(returnedFoods);
            Assert.Equal(2, returnedFoods.Length);
            Assert.Contains(returnedFoods, food => food.Id == createdFood.Id && food.Name == "Ketchup");
            Assert.Contains(returnedFoods, food => food.Id == secondFood.Id && food.Name == "Pizza");

            await using var verificationScope = app.Services.CreateAsyncScope();
            await using var verificationContext =
                verificationScope.ServiceProvider.GetRequiredService<TriggeredDbContext>();
            var persistedFood = await verificationContext.Foods
                .SingleAsync(food => food.Id == createdFood.Id);
            var persistedObservation = await verificationContext.Observations
                .Include(observation => observation.Food)
                .SingleAsync(observation => observation.Id == createdObservation.Id);

            Assert.Equal("Ketchup", persistedFood.Name);
            Assert.Equal(createdFood.Id, persistedObservation.FoodId);
            Assert.Equal(persistedFood.Id, persistedObservation.Food.Id);
            Assert.Equal(consumedAt, persistedObservation.ConsumedAt);
            Assert.Equal(reaction, persistedObservation.Reaction);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task Get_foods_returns_empty_collection_when_database_has_no_foods()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var app = await CreateApiAsync(connection);

        await app.StartAsync();
        try
        {
            var addresses = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!;
            using var client = new HttpClient
            {
                BaseAddress = new Uri(addresses.Addresses.Single())
            };

            var response = await client.GetAsync("/api/foods");
            var observationsResponse = await client.GetAsync("/api/observations");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty((await response.Content.ReadFromJsonAsync<FoodResponse[]>())!);
            Assert.Equal(HttpStatusCode.OK, observationsResponse.StatusCode);
            Assert.Empty((await observationsResponse.Content.ReadFromJsonAsync<ObservationResponse[]>())!);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task Get_food_by_id_returns_the_matching_food()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var app = await CreateApiAsync(connection);

        await app.StartAsync();
        try
        {
            var addresses = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!;
            using var client = new HttpClient
            {
                BaseAddress = new Uri(addresses.Addresses.Single())
            };
            var createResponse = await client.PostAsJsonAsync(
                "/api/foods",
                new CreateFoodRequest("Ketchup"));
            var createdFood = await createResponse.Content.ReadFromJsonAsync<CreatedFoodResponse>();
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            Assert.NotNull(createdFood);

            var response = await client.GetAsync($"/api/foods/{createdFood.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var food = await response.Content.ReadFromJsonAsync<FoodResponse>();
            Assert.NotNull(food);
            Assert.Equal(createdFood.Id, food.Id);
            Assert.Equal("Ketchup", food.Name);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task Get_food_by_id_returns_not_found_when_food_does_not_exist()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var app = await CreateApiAsync(connection);

        await app.StartAsync();
        try
        {
            var addresses = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!;
            using var client = new HttpClient
            {
                BaseAddress = new Uri(addresses.Addresses.Single())
            };

            var response = await client.GetAsync($"/api/foods/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task Get_food_by_id_returns_bad_request_for_invalid_id()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var app = await CreateApiAsync(connection);

        await app.StartAsync();
        try
        {
            var addresses = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!;
            using var client = new HttpClient
            {
                BaseAddress = new Uri(addresses.Addresses.Single())
            };

            var response = await client.GetAsync("/api/foods/not-a-guid");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task Swagger_documents_food_by_id_route_parameter_and_responses()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var app = await CreateApiAsync(connection);

        await app.StartAsync();
        try
        {
            var addresses = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!;
            using var client = new HttpClient
            {
                BaseAddress = new Uri(addresses.Addresses.Single())
            };

            var response = await client.GetAsync("/swagger/v1/swagger.json");
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var operation = document.RootElement
                .GetProperty("paths")
                .GetProperty("/api/foods/{id}")
                .GetProperty("get");
            var parameter = Assert.Single(operation.GetProperty("parameters").EnumerateArray());
            Assert.Equal("id", parameter.GetProperty("name").GetString());
            Assert.Equal("path", parameter.GetProperty("in").GetString());
            Assert.True(parameter.GetProperty("required").GetBoolean());
            Assert.Equal("string", parameter.GetProperty("schema").GetProperty("type").GetString());
            Assert.Equal("uuid", parameter.GetProperty("schema").GetProperty("format").GetString());

            var responses = operation.GetProperty("responses");
            Assert.True(responses.TryGetProperty("200", out var success));
            Assert.Contains(
                "FoodResponse",
                success.GetProperty("content")
                    .GetProperty("application/json")
                    .GetProperty("schema")
                    .GetRawText());
            Assert.True(responses.TryGetProperty("404", out _));

            var observationsOperation = document.RootElement
                .GetProperty("paths")
                .GetProperty("/api/observations")
                .GetProperty("get");
            var observationSuccess = observationsOperation.GetProperty("responses").GetProperty("200");
            Assert.Contains(
                "ObservationResponse",
                observationSuccess.GetProperty("content")
                    .GetProperty("application/json")
                    .GetProperty("schema")
                    .GetRawText());
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static async Task<WebApplication> CreateApiAsync(SqliteConnection connection)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddDbContext<TriggeredDbContext>(options => options.UseSqlite(connection));
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddScoped<IFoodRepository, FoodRepository>();
        builder.Services.AddScoped<IObservationRepository, ObservationRepository>();
        builder.Services.AddScoped<CreateFoodUseCase>();
        builder.Services.AddScoped<GetFoodsUseCase>();
        builder.Services.AddScoped<GetFoodByIdUseCase>();
        builder.Services.AddScoped<CreateObservationUseCase>();
        builder.Services.AddScoped<GetObservationsUseCase>();

        var app = builder.Build();
        app.UseSwagger();
        app.MapFoodEndpoints();
        app.MapObservationEndpoints();
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TriggeredDbContext>()
            .Database.EnsureCreatedAsync();
        return app;
    }
}
