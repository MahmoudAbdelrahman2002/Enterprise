using Enterprise.Application.Common.Models;

namespace Enterprise.Application.Features.Provider.Products.DTOs;

public sealed record ProductTranslationsDto(
    LocalizedText Name,
    LocalizedText? Description);
