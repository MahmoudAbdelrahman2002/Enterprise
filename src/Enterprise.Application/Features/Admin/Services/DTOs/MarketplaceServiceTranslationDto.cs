namespace Enterprise.Application.Features.Admin.Services.DTOs;

public sealed record MarketplaceServiceTranslationDto(
    string LanguageCode,
    string Name,
    string? Description);
