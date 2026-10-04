using System.ComponentModel.DataAnnotations;
using Triggered.Application.UseCases;

namespace Triggered.Presentation;

public sealed record CreateFoodRequest([property: Required] string Name);

public sealed record CreatedFoodResponse(Guid Id, string Name);

public sealed record FoodResponse(Guid Id, string Name);

public sealed record FoodTriggerPatternResponse(Guid FoodId, string Pattern);

public static class FoodEndpoints
{
    public static IEndpointRouteBuilder MapFoodEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/foods", GetFoodsAsync)
            .Produces<FoodResponse[]>(StatusCodes.Status200OK);

        endpoints.MapGet("/api/foods/{id}", GetFoodByIdAsync)
            .Produces<FoodResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        endpoints.MapGet("/api/foods/{foodId}/trigger-pattern", GetFoodTriggerPatternAsync)
            .Produces<FoodTriggerPatternResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

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

    public static async Task<IResult> GetFoodByIdAsync(
        Guid id,
        GetFoodByIdUseCase useCase,
        CancellationToken cancellationToken)
    {
        var food = await useCase.ExecuteAsync(id, cancellationToken);
        return food is null
            ? Results.NotFound()
            : Results.Ok(new FoodResponse(food.Id, food.Name));
    }

    public static async Task<IResult> GetFoodTriggerPatternAsync(
        Guid foodId,
        GetFoodByIdUseCase getFoodByIdUseCase,
        CalculateFoodTriggerPatternUseCase calculateFoodTriggerPatternUseCase,
        CancellationToken cancellationToken)
    {
        var food = await getFoodByIdUseCase.ExecuteAsync(foodId, cancellationToken);
        if (food is null)
        {
            return Results.NotFound();
        }

        var pattern = await calculateFoodTriggerPatternUseCase.ExecuteAsync(foodId, cancellationToken);
        var patternName = pattern.Level switch
        {
            FoodTriggerPatternLevel.InsufficientData => "Insufficient Data",
            FoodTriggerPatternLevel.Low => "Low",
            FoodTriggerPatternLevel.PossibleTrigger => "Possible Trigger",
            FoodTriggerPatternLevel.Moderate => "Moderate",
            FoodTriggerPatternLevel.LikelyTrigger => "Likely Trigger",
            FoodTriggerPatternLevel.StrongTriggerPattern => "Strong Trigger Pattern",
            _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern.Level, null)
        };

        return Results.Ok(new FoodTriggerPatternResponse(foodId, patternName));
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
