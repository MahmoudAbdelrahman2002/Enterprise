using System.Text.Json;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Common.Validation;
using FluentValidation;

namespace Enterprise.Application.Features.Client.WebHook.Commands.HandleStripeWebhookCommand;

public sealed class HandleStripeWebhookCommandValidator : AbstractValidator<HandleStripeWebhookCommand>
{
    public HandleStripeWebhookCommandValidator(IAppLocalizer localizer)
    {
        RuleFor(x => x.Signature).Required(localizer).MaxLen(localizer, ValidationPolicy.TokenMax);
        RuleFor(x => x.Json).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(_ => localizer[MessageKeys.Validation.Required])
            .MaximumLength(ValidationPolicy.WebhookMaxBytes).WithMessage(_ => localizer[MessageKeys.Validation.MaxLength, ValidationPolicy.WebhookMaxBytes])
            .Must(IsEventEnvelope).WithMessage(_ => localizer[MessageKeys.Validation.CheckRequest]);
    }

    private static bool IsEventEnvelope(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            return root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(id.GetString()) &&
                root.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(type.GetString()) &&
                root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object &&
                data.TryGetProperty("object", out var obj) && obj.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException) { return false; }
    }
}
