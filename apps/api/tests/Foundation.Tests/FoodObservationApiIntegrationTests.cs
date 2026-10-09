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
    public async Task Update_observation_persists_changes_and_refreshes_food_history_and_pattern()
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

            var consumedAt = new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);
            var observation1 = await CreateObservationAsync(client, food.Id, consumedAt, "Reflux");
            await CreateObservationAsync(client, food.Id, consumedAt.AddHours(1), "Reflux");
            await CreateObservationAsync(client, food.Id, consumedAt.AddHours(2), "Reflux");
            var initialPatternResponse = await client.GetAsync($"/api/foods/{food.Id}/trigger-pattern");
            var initialPattern =
                await initialPatternResponse.Content.ReadFromJsonAsync<FoodTriggerPatternResponse>();
            Assert.Equal(HttpStatusCode.OK, initialPatternResponse.StatusCode);
            Assert.NotNull(initialPattern);
            Assert.Equal("Strong Trigger Pattern", initialPattern.Pattern);

            var updatedConsumedAt = consumedAt.AddDays(1);
            var updateResponse = await client.PutAsJsonAsync(
                $"/api/observations/{observation1.Id}",
                new
                {
                    consumedAt = updatedConsumedAt,
                    reaction = "No reaction",
                    foodId = Guid.NewGuid()
                });

            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            var updatedObservation = await updateResponse.Content.ReadFromJsonAsync<ObservationResponse>();
            Assert.NotNull(updatedObservation);
            Assert.Equal(observation1.Id, updatedObservation.Id);
            Assert.Equal(food.Id, updatedObservation.FoodId);
            Assert.Equal(updatedConsumedAt, updatedObservation.ConsumedAt);
            Assert.Equal("No reaction", updatedObservation.Reaction);

            var historyResponse = await client.GetAsync($"/api/foods/{food.Id}/observations");
            var history = await historyResponse.Content.ReadFromJsonAsync<ObservationResponse[]>();
            Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
            Assert.NotNull(history);
            Assert.Equal(3, history.Length);
            var updatedHistoryObservation = Assert.Single(history, item => item.Id == observation1.Id);
            Assert.Equal(food.Id, updatedHistoryObservation.FoodId);
            Assert.Equal(updatedConsumedAt, updatedHistoryObservation.ConsumedAt);
            Assert.Equal("No reaction", updatedHistoryObservation.Reaction);

            var updatedPatternResponse = await client.GetAsync($"/api/foods/{food.Id}/trigger-pattern");
            var updatedPattern =
                await updatedPatternResponse.Content.ReadFromJsonAsync<FoodTriggerPatternResponse>();
            Assert.Equal(HttpStatusCode.OK, updatedPatternResponse.StatusCode);
            Assert.NotNull(updatedPattern);
            Assert.Equal("Likely Trigger", updatedPattern.Pattern);

            var missingObservationResponse = await client.PutAsJsonAsync(
                $"/api/observations/{Guid.NewGuid()}",
                new UpdateObservationRequest(consumedAt, "Reflux"));
            Assert.Equal(HttpStatusCode.NotFound, missingObservationResponse.StatusCode);

            var missingConsumedAtResponse = await client.PutAsJsonAsync(
                $"/api/observations/{observation1.Id}",
                new UpdateObservationRequest(default, "Reflux"));
            var missingReactionResponse = await client.PutAsJsonAsync(
                $"/api/observations/{observation1.Id}",
                new UpdateObservationRequest(consumedAt, " "));
            Assert.Equal(HttpStatusCode.BadRequest, missingConsumedAtResponse.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, missingReactionResponse.StatusCode);
            Assert.Equal("application/problem+json", missingReactionResponse.Content.Headers.ContentType?.MediaType);
            using var validationProblem =
                JsonDocument.Parse(await missingReactionResponse.Content.ReadAsStringAsync());
            Assert.Equal(400, validationProblem.RootElement.GetProperty("status").GetInt32());
            Assert.Equal(
                "Reaction is required.",
                validationProblem.RootElement.GetProperty("errors").GetProperty("Reaction")[0].GetString());

            await using var verificationScope = app.Services.CreateAsyncScope();
            await using var verificationContext =
                verificationScope.ServiceProvider.GetRequiredService<TriggeredDbContext>();
            Assert.Equal(3, await verificationContext.Observations.CountAsync());
            var persistedObservation = await verificationContext.Observations
                .SingleAsync(item => item.Id == observation1.Id);
            Assert.Equal(food.Id, persistedObservation.FoodId);
            Assert.Equal(updatedConsumedAt, persistedObservation.ConsumedAt);
            Assert.Equal("No reaction", persistedObservation.Reaction);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task Delete_observation_removes_it_from_history_and_pattern_without_deleting_food()
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

            var consumedAt = new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);
            var deletedObservation = await CreateObservationAsync(client, food.Id, consumedAt, "Reflux");
            var remainingObservation1 =
                await CreateObservationAsync(client, food.Id, consumedAt.AddHours(1), "Reflux");
            var remainingObservation2 =
                await CreateObservationAsync(client, food.Id, consumedAt.AddHours(2), "Reflux");
            var otherFood = await CreateFoodAsync(client, "Mustard", "Heartburn");

            var deleteResponse = await client.DeleteAsync($"/api/observations/{deletedObservation.Id}");
            var secondDeleteResponse = await client.DeleteAsync($"/api/observations/{deletedObservation.Id}");
            var unknownDeleteResponse = await client.DeleteAsync($"/api/observations/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, secondDeleteResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, unknownDeleteResponse.StatusCode);

            var historyResponse = await client.GetAsync($"/api/foods/{food.Id}/observations");
            var history = await historyResponse.Content.ReadFromJsonAsync<ObservationResponse[]>();
            Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
            Assert.NotNull(history);
            Assert.Equal(
                [remainingObservation2.Id, remainingObservation1.Id],
                history.Select(observation => observation.Id));

            var patternResponse = await client.GetAsync($"/api/foods/{food.Id}/trigger-pattern");
            var pattern = await patternResponse.Content.ReadFromJsonAsync<FoodTriggerPatternResponse>();
            Assert.Equal(HttpStatusCode.OK, patternResponse.StatusCode);
            Assert.NotNull(pattern);
            Assert.Equal("Insufficient Data", pattern.Pattern);

            await using var verificationScope = app.Services.CreateAsyncScope();
            await using var verificationContext =
                verificationScope.ServiceProvider.GetRequiredService<TriggeredDbContext>();
            Assert.True(await verificationContext.Foods.AnyAsync(item => item.Id == food.Id));
            Assert.True(await verificationContext.Foods.AnyAsync(item => item.Id == otherFood));
            Assert.False(await verificationContext.Observations
                .AnyAsync(item => item.Id == deletedObservation.Id));
            Assert.Equal(2, await verificationContext.Observations
                .CountAsync(item => item.FoodId == food.Id));
            Assert.Equal(1, await verificationContext.Observations
                .CountAsync(item => item.FoodId == otherFood));
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
            var paths = document.RootElement.GetProperty("paths");
            var foodCollection = paths.GetProperty("/api/foods");
            var listFoodsSuccess = foodCollection.GetProperty("get").GetProperty("responses").GetProperty("200");
            Assert.Contains(
                "FoodResponse",
                listFoodsSuccess.GetProperty("content").GetProperty("application/json")
                    .GetProperty("schema").GetRawText());
            var createFoodOperation = foodCollection.GetProperty("post");
            Assert.True(createFoodOperation.GetProperty("responses").TryGetProperty("201", out _));
            Assert.Contains(
                "CreatedFoodResponse",
                createFoodOperation.GetProperty("responses").GetProperty("201")
                    .GetProperty("content").GetProperty("application/json").GetProperty("schema").GetRawText());
            Assert.True(createFoodOperation.GetProperty("responses").TryGetProperty("400", out _));
            Assert.True(createFoodOperation.GetProperty("responses").GetProperty("400")
                .GetProperty("content").TryGetProperty("application/problem+json", out _));
            Assert.True(createFoodOperation.GetProperty("requestBody").GetProperty("required").GetBoolean());
            var createFoodSchemaName = createFoodOperation.GetProperty("requestBody")
                .GetProperty("content")
                .GetProperty("application/json")
                .GetProperty("schema")
                .GetProperty("$ref")
                .GetString()!
                .Split('/')
                .Last();
            var createFoodSchema = document.RootElement.GetProperty("components")
                .GetProperty("schemas")
                .GetProperty(createFoodSchemaName);
            Assert.Contains("name", createFoodSchema.GetProperty("required")
                .EnumerateArray().Select(property => property.GetString()));

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

            var deleteOperation = document.RootElement
                .GetProperty("paths")
                .GetProperty("/api/observations/{observationId}")
                .GetProperty("delete");
            var deleteParameter = Assert.Single(deleteOperation.GetProperty("parameters").EnumerateArray());
            Assert.Equal("observationId", deleteParameter.GetProperty("name").GetString());
            Assert.Equal("path", deleteParameter.GetProperty("in").GetString());
            var deleteResponses = deleteOperation.GetProperty("responses");
            Assert.True(deleteResponses.TryGetProperty("204", out _));
            Assert.True(deleteResponses.TryGetProperty("404", out _));

            var updateOperation = document.RootElement
                .GetProperty("paths")
                .GetProperty("/api/observations/{observationId}")
                .GetProperty("put");
            var updateParameter = Assert.Single(updateOperation.GetProperty("parameters").EnumerateArray());
            Assert.Equal("observationId", updateParameter.GetProperty("name").GetString());
            Assert.True(updateOperation.TryGetProperty("requestBody", out var requestBody));
            Assert.True(requestBody.GetProperty("required").GetBoolean());
            var requestSchema = requestBody.GetProperty("content")
                .GetProperty("application/json")
                .GetProperty("schema");
            var updateRequestSchemaName = requestSchema.GetProperty("$ref").GetString()!
                .Split('/').Last();
            var updateRequestSchema = document.RootElement
                .GetProperty("components")
                .GetProperty("schemas")
                .GetProperty(updateRequestSchemaName);
            Assert.Equal(
                ["consumedAt", "reaction"],
                updateRequestSchema.GetProperty("properties").EnumerateObject()
                    .Select(property => property.Name).Order().ToArray());
            Assert.Equal(
                ["consumedAt", "reaction"],
                updateRequestSchema.GetProperty("required").EnumerateArray()
                    .Select(property => property.GetString()).Order().ToArray());
            var updateResponses = updateOperation.GetProperty("responses");
            Assert.True(updateResponses.TryGetProperty("200", out var updateSuccess));
            Assert.Contains(
                "ObservationResponse",
                updateSuccess.GetProperty("content")
                    .GetProperty("application/json")
                    .GetProperty("schema")
                    .GetRawText());
            Assert.True(updateResponses.TryGetProperty("400", out _));
            Assert.True(updateResponses.TryGetProperty("404", out _));

            var createObservationOperation = paths.GetProperty("/api/observations").GetProperty("post");
            Assert.True(createObservationOperation.GetProperty("responses").TryGetProperty("201", out _));
            Assert.Contains(
                "CreatedObservationResponse",
                createObservationOperation.GetProperty("responses").GetProperty("201")
                    .GetProperty("content").GetProperty("application/json").GetProperty("schema").GetRawText());
            Assert.True(createObservationOperation.GetProperty("responses").TryGetProperty("400", out _));
            Assert.True(createObservationOperation.GetProperty("responses").TryGetProperty("404", out _));
            Assert.True(createObservationOperation.GetProperty("responses").GetProperty("400")
                .GetProperty("content").TryGetProperty("application/problem+json", out _));
            var createObservationRequestBody = createObservationOperation.GetProperty("requestBody");
            Assert.True(
                createObservationRequestBody.TryGetProperty("required", out var requestBodyRequired),
                createObservationRequestBody.GetRawText());
            Assert.True(requestBodyRequired.GetBoolean());
            var createObservationSchemaName = createObservationRequestBody
                .GetProperty("content")
                .GetProperty("application/json")
                .GetProperty("schema")
                .GetProperty("$ref")
                .GetString()!
                .Split('/')
                .Last();
            var createObservationSchema = document.RootElement.GetProperty("components")
                .GetProperty("schemas")
                .GetProperty(createObservationSchemaName);
            Assert.Equal(
                ["consumedAt", "foodId", "reaction"],
                createObservationSchema.GetProperty("required").EnumerateArray()
                    .Select(property => property.GetString()).Order().ToArray());
            Assert.True(paths.GetProperty("/api/observations").GetProperty("get")
                .GetProperty("responses").TryGetProperty("200", out _));
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
        builder.Services.AddSwaggerGen(options =>
            options.OperationFilter<ObservationRequestBodyRequiredOperationFilter>());
        builder.Services.AddScoped<IFoodRepository, FoodRepository>();
        builder.Services.AddScoped<IObservationRepository, ObservationRepository>();
        builder.Services.AddScoped<CreateFoodUseCase>();
        builder.Services.AddScoped<GetFoodsUseCase>();
        builder.Services.AddScoped<GetFoodByIdUseCase>();
        builder.Services.AddScoped<CreateObservationUseCase>();
        builder.Services.AddScoped<UpdateObservationUseCase>();
        builder.Services.AddScoped<DeleteObservationUseCase>();
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
