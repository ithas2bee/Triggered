using System.ComponentModel.DataAnnotations;
using Triggered.Application.UseCases;

namespace Triggered.Presentation;

public sealed record CreateFoodRequest([property: Required] string Name);

public sealed record CreatedFoodResponse(Guid Id, string Name);

public sealed record FoodResponse(Guid Id, string Name);

public static class FoodEndpoints
{
    public static IEndpointRouteBuilder MapFoodEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/foods", GetFoodsAsync)
            .Produces<FoodResponse[]>(StatusCodes.Status200OK);

        endpoints.MapPost("/api/foods", CreateFoodAsync)
            .Accepts<CreateFoodRequest>(false, "application/json")
            .Produces<CreatedFoodResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        return endpoints;
    }

    public static async Task<IResult> GetFoodsAsync(
        GetFoodsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var foods = await useCase.ExecuteAsync(cancellationToken);
        return Results.Ok(foods.Select(food => new FoodResponse(food.Id, food.Name)).ToArray());
    }

    public static async Task<IResult> CreateFoodAsync(
        CreateFoodRequest request,
        CreateFoodUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["A request body is required."]
            });
        }

        try
        {
            var food = await useCase.ExecuteAsync(request.Name, cancellationToken);
            return Results.Created(
                (string?)null,
                new CreatedFoodResponse(food.Id, food.Name));
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [exception.ParamName ?? nameof(request.Name)] = [exception.Message]
            });
        }
    }
}
