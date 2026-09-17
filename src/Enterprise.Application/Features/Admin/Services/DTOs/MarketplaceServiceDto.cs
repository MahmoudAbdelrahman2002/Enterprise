using Enterprise.Application.Common.Models;

namespace Enterprise.Application.Features.Admin.Services.DTOs;

public sealed record MarketplaceServiceTranslationsDto(
    LocalizedText Name,
    LocalizedText? Description);

public sealed record MarketplaceServiceDto(
    Guid Id,
    string Code,
    bool IsActive,
    int DisplayOrder,
    string Name,
    string? Description,
    MarketplaceServiceTranslationsDto Translations,
    DateTime CreatedAtUtc,
    DateTime? LastModifiedAtUtc);
