using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Triggered.Application.Abstractions;
using Triggered.Application.UseCases;
using Triggered.Domain;
using Triggered.Presentation;

namespace Foundation.Tests;

public sealed class FoodEndpointTests
{
    [Fact]
    public void MapFoodEndpoints_maps_post_api_route_and_documents_response()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddScoped<CreateFoodUseCase>();
        using var app = builder.Build();

        app.MapFoodEndpoints();

        var endpoint = Assert.Single(
            ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(source => source.Endpoints)
                .OfType<RouteEndpoint>());
        Assert.Equal("/api/foods", endpoint.RoutePattern.RawText);
        Assert.Contains("POST", endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
        var request = endpoint.Metadata.GetMetadata<IAcceptsMetadata>();
        Assert.NotNull(request);
        Assert.Equal(typeof(CreateFoodRequest), request.RequestType);
        Assert.False(request.IsOptional);
        var response = Assert.Single(
            endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(),
            metadata => metadata.StatusCode == StatusCodes.Status201Created);
        Assert.Equal(typeof(CreatedFoodResponse), response.Type);
        Assert.Contains(
            endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(),
            metadata => metadata.StatusCode == StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task CreateFoodAsync_creates_food_and_returns_created_response()
    {
        var foodRepository = new FakeFoodRepository();
        var useCase = new CreateFoodUseCase(foodRepository);

        var result = await FoodEndpoints.CreateFoodAsync(
            new CreateFoodRequest("Ketchup"),
            useCase,
            CancellationToken.None);
        var response = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status201Created, response.StatusCode);
        var created = await JsonSerializer.DeserializeAsync<CreatedFoodResponse>(
            response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var persisted = Assert.Single(foodRepository.Foods);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(persisted.Id, created.Id);
        Assert.Equal("Ketchup", created.Name);
    }

    [Fact]
    public async Task CreateFoodAsync_returns_bad_request_when_body_is_missing()
    {
        var foodRepository = new FakeFoodRepository();
        var useCase = new CreateFoodUseCase(foodRepository);

        var result = await FoodEndpoints.CreateFoodAsync(null!, useCase, CancellationToken.None);
        var response = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Empty(foodRepository.Foods);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(null)]
    public async Task CreateFoodAsync_returns_bad_request_for_invalid_name(string? name)
    {
        var foodRepository = new FakeFoodRepository();
        var useCase = new CreateFoodUseCase(foodRepository);

        var result = await FoodEndpoints.CreateFoodAsync(
            new CreateFoodRequest(name!),
            useCase,
            CancellationToken.None);
        var response = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Empty(foodRepository.Foods);
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

    private sealed class FakeFoodRepository : IFoodRepository
    {
        public List<Food> Foods { get; } = [];

        public Task<Food?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Foods.SingleOrDefault(food => food.Id == id));
        }

        public Task AddAsync(Food food, CancellationToken cancellationToken = default)
        {
            Foods.Add(food);
            return Task.CompletedTask;
        }
    }
}
