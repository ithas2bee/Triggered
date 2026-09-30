using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Triggered.Application.Abstractions;
using Triggered.Application.UseCases;
using Triggered.Domain;
using Triggered.Presentation;

namespace Foundation.Tests;

public sealed class ObservationEndpointTests
{
    [Fact]
    public void MapObservationEndpoints_maps_post_api_route()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddScoped<CreateObservationUseCase>();
        using var app = builder.Build();

        app.MapObservationEndpoints();

        var endpoint = Assert.Single(
            ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(source => source.Endpoints)
                .OfType<RouteEndpoint>());
        Assert.Equal("/api/observations", endpoint.RoutePattern.RawText);
        Assert.Contains("POST", endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task CreateObservationAsync_creates_observation_and_returns_created_response()
    {
        var food = new Food("Ketchup");
        var observationRepository = new FakeObservationRepository();
        var useCase = new CreateObservationUseCase(
            new FakeFoodRepository(food),
            observationRepository);
        var consumedAt = new DateTimeOffset(2026, 9, 29, 12, 30, 0, TimeSpan.Zero);

        var result = await ObservationEndpoints.CreateObservationAsync(
            new CreateObservationRequest(food.Id, consumedAt, "Heartburn"),
            useCase,
            CancellationToken.None);
        var response = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status201Created, response.StatusCode);
        var created = await JsonSerializer.DeserializeAsync<CreatedObservationResponse>(
            response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(food.Id, created.FoodId);
        Assert.Equal(consumedAt, created.ConsumedAt);
        Assert.Equal("Heartburn", created.Reaction);
        Assert.Equal(created.Id, Assert.Single(observationRepository.Observations).Id);
    }

    [Fact]
    public async Task CreateObservationAsync_returns_not_found_when_food_does_not_exist()
    {
        var observationRepository = new FakeObservationRepository();
        var useCase = new CreateObservationUseCase(
            new FakeFoodRepository(null),
            observationRepository);

        var result = await ObservationEndpoints.CreateObservationAsync(
            new CreateObservationRequest(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                "Heartburn"),
            useCase,
            CancellationToken.None);
        var response = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
        Assert.Empty(observationRepository.Observations);
    }

    [Fact]
    public async Task CreateObservationAsync_returns_bad_request_for_invalid_observation_data()
    {
        var food = new Food("Ketchup");
        var foodRepository = new FakeFoodRepository(food);
        var observationRepository = new FakeObservationRepository();
        var useCase = new CreateObservationUseCase(foodRepository, observationRepository);

        var result = await ObservationEndpoints.CreateObservationAsync(
            new CreateObservationRequest(food.Id, default, " "),
            useCase,
            CancellationToken.None);
        var response = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Equal(0, foodRepository.LookupCount);
        Assert.Empty(observationRepository.Observations);
    }

    [Fact]
    public async Task CreateObservationAsync_translates_domain_validation_to_bad_request()
    {
        var useCase = new CreateObservationUseCase(
            new ThrowingFoodRepository(),
            new FakeObservationRepository());

        var result = await ObservationEndpoints.CreateObservationAsync(
            new CreateObservationRequest(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                "Heartburn"),
            useCase,
            CancellationToken.None);
        var response = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
    }

    private static async Task<(int StatusCode, MemoryStream Body)> ExecuteAsync(IResult result)
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .ConfigureHttpJsonOptions(_ => { })
            .BuildServiceProvider();
        var context = new DefaultHttpContext();
        context.RequestServices = services;
        var body = new MemoryStream();
        context.Response.Body = body;

        await result.ExecuteAsync(context);

        body.Position = 0;
        return (context.Response.StatusCode, body);
    }

    private sealed class FakeFoodRepository(Food? food) : IFoodRepository
    {
        public int LookupCount { get; private set; }

        public Task<Food?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            LookupCount++;
            return Task.FromResult(food?.Id == id ? food : null);
        }
    }

    private sealed class ThrowingFoodRepository : IFoodRepository
    {
        public Task<Food?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            throw new ArgumentException("Consumption time is required.", "consumedAt");
        }
    }

    private sealed class FakeObservationRepository : IObservationRepository
    {
        public List<Observation> Observations { get; } = [];

        public Task AddAsync(Observation observation, CancellationToken cancellationToken = default)
        {
            Observations.Add(observation);
            return Task.CompletedTask;
        }
    }
}
