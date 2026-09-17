namespace Enterprise.Application.Features.Admin.Services.DTOs;

public sealed record MarketplaceServiceLookupDto(
    Guid Id,
    string Code,
    string Name,
    string? Description);
