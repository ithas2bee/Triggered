using Swashbuckle.AspNetCore.SwaggerGen;

namespace Triggered.Presentation;

public sealed class ObservationRequestBodyRequiredOperationFilter : IOperationFilter
{
    public void Apply(Microsoft.OpenApi.OpenApiOperation operation, OperationFilterContext context)
    {
        var isObservationWrite = context.ApiDescription.HttpMethod is "POST" or "PUT"
            && context.ApiDescription.RelativePath?.StartsWith("api/observations", StringComparison.Ordinal) == true;
        if (isObservationWrite && operation.RequestBody is not null)
        {
            operation.RequestBody = new Microsoft.OpenApi.OpenApiRequestBody
            {
                Description = operation.RequestBody.Description,
                Content = operation.RequestBody.Content,
                Extensions = operation.RequestBody.Extensions,
                Required = true
            };
        }
    }
}
