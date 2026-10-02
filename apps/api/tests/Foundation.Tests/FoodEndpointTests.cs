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
    public void MapFoodEndpoints_maps_get_and_post_routes_and_documents_responses()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddScoped<CreateFoodUseCase>();
        builder.Services.AddScoped<GetFoodsUseCase>();
        builder.Services.AddScoped<GetFoodByIdUseCase>();
        using var app = builder.Build();

        app.MapFoodEndpoints();

        var endpoints =
            ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(source => source.Endpoints)
                .OfType<RouteEndpoint>()
                .ToArray();
        var getEndpoint = Assert.Single(
            endpoints,
            endpoint => endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Contains("GET")
                && endpoint.RoutePattern.RawText == "/api/foods");
        Assert.Equal("/api/foods", getEndpoint.RoutePattern.RawText);
        var getResponse = Assert.Single(
            getEndpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(),
            metadata => metadata.StatusCode == StatusCodes.Status200OK);
        Assert.Equal(typeof(FoodResponse[]), getResponse.Type);

        var getFoodByIdEndpoint = Assert.Single(
            endpoints,
            endpoint => endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Contains("GET")
                && endpoint.RoutePattern.RawText == "/api/foods/{id}");
        Assert.Contains(getFoodByIdEndpoint.RoutePattern.Parameters, parameter => parameter.Name == "id");
        Assert.Contains(
            getFoodByIdEndpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(),
            metadata => metadata.StatusCode == StatusCodes.Status200OK
                && metadata.Type == typeof(FoodResponse));
        Assert.Contains(
            getFoodByIdEndpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(),
            metadata => metadata.StatusCode == StatusCodes.Status404NotFound);
        Assert.Contains(
            getFoodByIdEndpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(),
            metadata => metadata.StatusCode == StatusCodes.Status400BadRequest);

        var postEndpoint = Assert.Single(
            endpoints,
            endpoint => endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Contains("POST"));
        Assert.Equal("/api/foods", postEndpoint.RoutePattern.RawText);
        var request = postEndpoint.Metadata.GetMetadata<IAcceptsMetadata>();
        Assert.NotNull(request);
        Assert.Equal(typeof(CreateFoodRequest), request.RequestType);
        Assert.False(request.IsOptional);
        var response = Assert.Single(
            postEndpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(),
            metadata => metadata.StatusCode == StatusCodes.Status201Created);
        Assert.Equal(typeof(CreatedFoodResponse), response.Type);
        Assert.Contains(
            postEndpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(),
            metadata => metadata.StatusCode == StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task GetFoodsAsync_returns_food_ids_and_names()
    {
        var foods = new[] { new Food("Ketchup"), new Food("Pizza") };
        var useCase = new GetFoodsUseCase(new FakeFoodRepository(foods));

        var result = await FoodEndpoints.GetFoodsAsync(useCase, CancellationToken.None);
        var response = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        var returnedFoods = await JsonSerializer.DeserializeAsync<FoodResponse[]>(
            response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(returnedFoods);
        Assert.Equal(foods.Length, returnedFoods.Length);
        foreach (var food in foods)
        {
            Assert.Contains(returnedFoods, returnedFood =>
                returnedFood.Id == food.Id && returnedFood.Name == food.Name);
        }
    }

    [Fact]
    public async Task GetFoodsAsync_returns_empty_collection_when_no_foods_exist()
    {
        var useCase = new GetFoodsUseCase(new FakeFoodRepository([]));

        var result = await FoodEndpoints.GetFoodsAsync(useCase, CancellationToken.None);
        var response = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        var returnedFoods = await JsonSerializer.DeserializeAsync<FoodResponse[]>(
            response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Empty(returnedFoods!);
    }

    [Fact]
    public async Task GetFoodByIdAsync_returns_food_response_with_only_id_and_name()
    {
        var foods = new[] { new Food("Ketchup"), new Food("Pizza") };
        var useCase = new GetFoodByIdUseCase(new FakeFoodRepository(foods));

        var result = await FoodEndpoints.GetFoodByIdAsync(
            foods[0].Id,
            useCase,
            CancellationToken.None);
        var response = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        using var json = await JsonDocument.ParseAsync(response.Body);
        var properties = json.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray();
        Assert.Equal(["id", "name"], properties);
        Assert.Equal(foods[0].Id, json.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("Ketchup", json.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetFoodByIdAsync_returns_not_found_when_food_does_not_exist()
    {
        var useCase = new GetFoodByIdUseCase(new FakeFoodRepository());

        var result = await FoodEndpoints.GetFoodByIdAsync(Guid.NewGuid(), useCase, CancellationToken.None);
        var response = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
        Assert.Equal(0, response.Body.Length);
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

        public FakeFoodRepository(IEnumerable<Food>? foods = null)
        {
            if (foods is not null)
            {
                Foods.AddRange(foods);
            }
        }

        public Task<Food?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Foods.SingleOrDefault(food => food.Id == id));
        }

        public Task<IReadOnlyList<Food>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Food>>(Foods);
        }

        public Task AddAsync(Food food, CancellationToken cancellationToken = default)
        {
            Foods.Add(food);
            return Task.CompletedTask;
        }
    }
}
