using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Client.Services.Queries.GetClientServiceByIdQuery;

public class GetClientServiceByIdQueryHandler(IUnitOfWork unitOfWork, ICurrentCulture culture) : IRequestHandler<GetClientServiceByIdQuery, ClientMarketServiceDto>
{
    public async Task<ClientMarketServiceDto> Handle(GetClientServiceByIdQuery request, CancellationToken cancellationToken)
    {
        var language = culture.LanguageCode;
        var service = await unitOfWork.Services.GetByIdAsync(request.ServiceId, cancellationToken);
        if (service is null)
        {
            throw new NotFoundException("Service not found");
        }
        return service.ClientMarketToDto(language);
    }
}
