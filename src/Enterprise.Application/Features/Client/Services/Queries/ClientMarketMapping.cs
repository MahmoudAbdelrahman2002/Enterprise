using Enterprise.Domain.Entities;

namespace Enterprise.Application.Features.Client.Services.Queries;

public static class ClientMarketMapping
{
     public static ClientMarketServiceDto ClientMarketToDto(this MarketplaceService service, string? currentLanguage = null)
    {
        var (name, description) = service.ResolveContent(currentLanguage);

        return new ClientMarketServiceDto
        {
            Id = service.Id,
            Name = name,
            Description = description,
            ImageUrl = service.ImageUrl
        };


    }


}
