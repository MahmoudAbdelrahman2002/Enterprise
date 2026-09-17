using MediatR;

namespace Enterprise.Application.Features.Admin.Services.Commands.DeleteMarketplaceService;

public sealed record DeleteMarketplaceServiceCommand(Guid Id) : IRequest;
