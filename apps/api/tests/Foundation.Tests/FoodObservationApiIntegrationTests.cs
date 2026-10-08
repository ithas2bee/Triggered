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
    public async Task Get_food_trigger_pattern_returns_calculated_levels_and_handles_invalid_food_ids()
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
            var likelyFoodId = await CreateFoodAsync(
                client,
                "Ketchup",
                "Reflux",
                "Reflux",
                "No reaction",
                "Reflux");
            var insufficientFoodId = await CreateFoodAsync(client, "Pizza", "Reflux", "Bloating");
            var noObservationsFoodId = await CreateFoodAsync(client, "Rice");

            foreach (var (foodId, expectedPattern) in new[]
            {
                (likelyFoodId, "Likely Trigger"),
                (insufficientFoodId, "Insufficient Data"),
                (noObservationsFoodId, "Insufficient Data")
            })
            {
                var response = await client.GetAsync($"/api/foods/{foodId}/trigger-pattern");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var pattern = await response.Content.ReadFromJsonAsync<FoodTriggerPatternResponse>();
                Assert.NotNull(pattern);
                Assert.Equal(foodId, pattern.FoodId);
                Assert.Equal(expectedPattern, pattern.Pattern);
            }

            var unknownFoodResponse =
                await client.GetAsync($"/api/foods/{Guid.NewGuid()}/trigger-pattern");
            var invalidFoodIdResponse =
                await client.GetAsync("/api/foods/not-a-guid/trigger-pattern");

            Assert.Equal(HttpStatusCode.NotFound, unknownFoodResponse.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, invalidFoodIdResponse.StatusCode);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task Get_food_observations_returns_only_food_history_newest_first_and_handles_missing_foods()
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
            var food = await foodResponse.Content.ReadFromJsonAsync<CreatedFoodResponse>();
            Assert.Equal(HttpStatusCode.Created, foodResponse.StatusCode);
            Assert.NotNull(food);

            var otherFoodResponse = await client.PostAsJsonAsync(
                "/api/foods",
                new CreateFoodRequest("Mustard"));
            var otherFood = await otherFoodResponse.Content.ReadFromJsonAsync<CreatedFoodResponse>();
            Assert.Equal(HttpStatusCode.Created, otherFoodResponse.StatusCode);
            Assert.NotNull(otherFood);

            var earlierConsumedAt = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
            var laterConsumedAt = new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);
            var earlierObservation = await CreateObservationAsync(
                client,
                food.Id,
                earlierConsumedAt,
                "No reaction");
            var laterObservation = await CreateObservationAsync(
                client,
                food.Id,
                laterConsumedAt,
                "Reflux");
            await CreateObservationAsync(
                client,
                otherFood.Id,
                laterConsumedAt.AddDays(1),
                "Bloating");

            var response = await client.GetAsync($"/api/foods/{food.Id}/observations");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var observations = await response.Content.ReadFromJsonAsync<ObservationResponse[]>();
            Assert.NotNull(observations);
            Assert.Equal([laterObservation.Id, earlierObservation.Id], observations.Select(item => item.Id));
            Assert.Equal(food.Id, observations[0].FoodId);
            Assert.Equal(laterConsumedAt, observations[0].ConsumedAt);
            Assert.Equal("Reflux", observations[0].Reaction);

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(
                ["consumedAt", "foodId", "id", "reaction"],
                json.RootElement[0].EnumerateObject().Select(property => property.Name).Order().ToArray());

            var emptyFoodResponse = await client.PostAsJsonAsync(
                "/api/foods",
                new CreateFoodRequest("Rice"));
            var emptyFood = await emptyFoodResponse.Content.ReadFromJsonAsync<CreatedFoodResponse>();
            Assert.Equal(HttpStatusCode.Created, emptyFoodResponse.StatusCode);
            Assert.NotNull(emptyFood);

            var emptyHistoryResponse = await client.GetAsync($"/api/foods/{emptyFood.Id}/observations");
            Assert.Equal(HttpStatusCode.OK, emptyHistoryResponse.StatusCode);
            Assert.Empty((await emptyHistoryResponse.Content.ReadFromJsonAsync<ObservationResponse[]>())!);

            var unknownFoodResponse =
                await client.GetAsync($"/api/foods/{Guid.NewGuid()}/observations");
            var invalidFoodIdResponse =
                await client.GetAsync("/api/foods/not-a-guid/observations");
            Assert.Equal(HttpStatusCode.NotFound, unknownFoodResponse.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, invalidFoodIdResponse.StatusCode);
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

            var patternOperation = document.RootElement
                .GetProperty("paths")
                .GetProperty("/api/foods/{foodId}/trigger-pattern")
                .GetProperty("get");
            var patternParameter = Assert.Single(patternOperation.GetProperty("parameters").EnumerateArray());
            Assert.Equal("foodId", patternParameter.GetProperty("name").GetString());
            var patternResponses = patternOperation.GetProperty("responses");
            Assert.True(patternResponses.TryGetProperty("200", out var patternSuccess));
            Assert.Contains(
                "FoodTriggerPatternResponse",
                patternSuccess.GetProperty("content")
                    .GetProperty("application/json")
                    .GetProperty("schema")
                    .GetRawText());
            Assert.True(patternResponses.TryGetProperty("404", out _));
            Assert.True(patternResponses.TryGetProperty("400", out _));

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

            var foodObservationsOperation = document.RootElement
                .GetProperty("paths")
                .GetProperty("/api/foods/{foodId}/observations")
                .GetProperty("get");
            var foodObservationsParameter =
                Assert.Single(foodObservationsOperation.GetProperty("parameters").EnumerateArray());
            Assert.Equal("foodId", foodObservationsParameter.GetProperty("name").GetString());
            Assert.Equal("path", foodObservationsParameter.GetProperty("in").GetString());
            var foodObservationsResponses = foodObservationsOperation.GetProperty("responses");
            Assert.True(foodObservationsResponses.TryGetProperty("200", out var foodObservationsSuccess));
            Assert.Contains(
                "ObservationResponse",
                foodObservationsSuccess.GetProperty("content")
                    .GetProperty("application/json")
                    .GetProperty("schema")
                    .GetRawText());
            Assert.True(foodObservationsResponses.TryGetProperty("404", out _));
            Assert.True(foodObservationsResponses.TryGetProperty("400", out _));
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
        builder.Services.AddScoped<GetFoodObservationsUseCase>();
        builder.Services.AddScoped<CalculateFoodTriggerPatternUseCase>();

        var app = builder.Build();
        app.UseSwagger();
        app.MapFoodEndpoints();
        app.MapObservationEndpoints();
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TriggeredDbContext>()
            .Database.EnsureCreatedAsync();
        return app;
    }

    private static async Task<CreatedObservationResponse> CreateObservationAsync(
        HttpClient client,
        Guid foodId,
        DateTimeOffset consumedAt,
        string reaction)
    {
        var response = await client.PostAsJsonAsync(
            "/api/observations",
            new CreateObservationRequest(foodId, consumedAt, reaction));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var observation = await response.Content.ReadFromJsonAsync<CreatedObservationResponse>();
        Assert.NotNull(observation);
        return observation;
    }

    private static async Task<Guid> CreateFoodAsync(
        HttpClient client,
        string name,
        params string[] reactions)
    {
        var foodResponse = await client.PostAsJsonAsync("/api/foods", new CreateFoodRequest(name));
        Assert.Equal(HttpStatusCode.Created, foodResponse.StatusCode);
        var food = await foodResponse.Content.ReadFromJsonAsync<CreatedFoodResponse>();
        Assert.NotNull(food);

        var consumedAt = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        foreach (var reaction in reactions)
        {
            var observationResponse = await client.PostAsJsonAsync(
                "/api/observations",
                new CreateObservationRequest(food.Id, consumedAt, reaction));
            Assert.Equal(HttpStatusCode.Created, observationResponse.StatusCode);
        }

        return food.Id;
    }
}
