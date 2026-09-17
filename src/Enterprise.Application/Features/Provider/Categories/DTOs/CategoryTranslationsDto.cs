using Enterprise.Application.Common.Models;

namespace Enterprise.Application.Features.Provider.Categories.DTOs;

public sealed record CategoryTranslationsDto(
    LocalizedText Name,
    LocalizedText? Description);
