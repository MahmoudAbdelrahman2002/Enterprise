using Enterprise.Application.Features.Orders.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Client.Orders.Queries.GetClientOrderBySession;

public sealed record GetClientOrderBySessionQuery(string SessionId) : IRequest<OrderDetailDto>;
