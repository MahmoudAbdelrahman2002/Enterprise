using Enterprise.Api.Models;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Api.Extensions;

public static class ApiValidationExtensions
{
    public static IMvcBuilder AddValidationResponses(this IMvcBuilder builder) => builder.ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var localizer = context.HttpContext.RequestServices.GetRequiredService<IAppLocalizer>();
            var fields = context.ModelState.Where(pair => pair.Value?.Errors.Count > 0)
                .ToDictionary(pair => pair.Key.TrimStart('$', '.'), pair => pair.Value!.Errors
                    .Select(error => error.Exception is not null || error.ErrorMessage.Contains("JSON", StringComparison.OrdinalIgnoreCase)
                        ? localizer[MessageKeys.Validation.AllowedValue]
                        : string.IsNullOrWhiteSpace(error.ErrorMessage) ? localizer[MessageKeys.Validation.CheckRequest] : error.ErrorMessage)
                    .Distinct().ToArray());
            return new BadRequestObjectResult(ApiResponse<object?>.Fail(400, localizer[MessageKeys.Validation.Failed],
                fields.SelectMany(pair => pair.Value), context.HttpContext.TraceIdentifier, fields));
        };
    });
}
