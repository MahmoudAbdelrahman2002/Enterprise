using MediatR;

namespace Enterprise.Application.Features.Client.Services.Queries.GetClientServiceByIdQuery;

public class GetClientServiceByIdQuery(Guid ServiceId) : IRequest<ClientMarketServiceDto>
{
    public Guid ServiceId { get; init; } = ServiceId;
}
