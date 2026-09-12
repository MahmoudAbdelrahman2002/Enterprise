using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Enterprise.Api.Extensions;

public sealed class AcceptLanguageHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];
        if (operation.Parameters.Any(p =>
                p.Name.Equals("Accept-Language", StringComparison.OrdinalIgnoreCase)
                && p.In == ParameterLocation.Header))
        {
            return;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Accept-Language",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Response language: en, it, or ar. Defaults to en. You can also pass ?culture=it.",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Enum =
                [
                    new Microsoft.OpenApi.Any.OpenApiString("en"),
                    new Microsoft.OpenApi.Any.OpenApiString("it"),
                    new Microsoft.OpenApi.Any.OpenApiString("ar")
                ]
            }
        });
    }
}
