using Triggered.Application.UseCases;

namespace Triggered.Presentation;

public sealed record CreateObservationRequest(
    Guid FoodId,
    DateTimeOffset ConsumedAt,
    string? Reaction);

public sealed record UpdateObservationRequest(
    DateTimeOffset ConsumedAt,
    string? Reaction);

public sealed record CreatedObservationResponse(
    Guid Id,
    Guid FoodId,
    DateTimeOffset ConsumedAt,
    string Reaction);

public sealed record ObservationResponse(
    Guid Id,
    Guid FoodId,
    DateTimeOffset ConsumedAt,
    string Reaction);

public static class ObservationEndpoints
{
    public static IEndpointRouteBuilder MapObservationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/observations", GetObservationsAsync)
            .Produces<ObservationResponse[]>(StatusCodes.Status200OK);

        endpoints.MapPost("/api/observations", CreateObservationAsync)
            .Accepts<CreateObservationRequest>("application/json")
            .Produces<CreatedObservationResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        endpoints.MapPut("/api/observations/{observationId:guid}", UpdateObservationAsync)
            .Accepts<UpdateObservationRequest>("application/json")
            .Produces<ObservationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        endpoints.MapDelete("/api/observations/{observationId:guid}", DeleteObservationAsync)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    public static async Task<IResult> GetObservationsAsync(
        GetObservationsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var observations = await useCase.ExecuteAsync(cancellationToken);
        return Results.Ok(observations.Select(observation => new ObservationResponse(
            observation.Id,
            observation.FoodId,
            observation.ConsumedAt,
            observation.Reaction)).ToArray());
    }

    public static async Task<IResult> CreateObservationAsync(
        CreateObservationRequest? request,
        CreateObservationUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["A request body is required."]
            });
        }

        var errors = new Dictionary<string, string[]>();
        if (request.FoodId == Guid.Empty)
        {
            errors[nameof(request.FoodId)] = ["Food ID is required."];
        }

        if (request.ConsumedAt == default)
        {
            errors[nameof(request.ConsumedAt)] = ["Consumption time is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Reaction))
        {
            errors[nameof(request.Reaction)] = ["Reaction is required."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        try
        {
            var observation = await useCase.ExecuteAsync(
                request.FoodId,
                request.ConsumedAt,
                request.Reaction!,
                cancellationToken);

            if (observation is null)
            {
                return Results.NotFound();
            }

            var response = new CreatedObservationResponse(
                observation.Id,
                observation.FoodId,
                observation.ConsumedAt,
                observation.Reaction);

            return Results.Created((string?)null, response);
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [exception.ParamName ?? "observation"] = [exception.Message]
            });
        }
    }

    public static async Task<IResult> UpdateObservationAsync(
        Guid observationId,
        UpdateObservationRequest? request,
        UpdateObservationUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["A request body is required."]
            });
        }

        var errors = new Dictionary<string, string[]>();
        if (request.ConsumedAt == default)
        {
            errors[nameof(request.ConsumedAt)] = ["Consumption time is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Reaction))
        {
            errors[nameof(request.Reaction)] = ["Reaction is required."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        try
        {
            var observation = await useCase.ExecuteAsync(
                observationId,
                request.ConsumedAt,
                request.Reaction,
                cancellationToken);

            if (observation is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new ObservationResponse(
                observation.Id,
                observation.FoodId,
                observation.ConsumedAt,
                observation.Reaction));
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [exception.ParamName ?? "observation"] = [exception.Message]
            });
        }
    }

    public static async Task<IResult> DeleteObservationAsync(
        Guid observationId,
        DeleteObservationUseCase useCase,
        CancellationToken cancellationToken)
    {
        var deleted = await useCase.ExecuteAsync(observationId, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
