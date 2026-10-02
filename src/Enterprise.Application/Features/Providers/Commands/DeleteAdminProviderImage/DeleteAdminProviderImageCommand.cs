using Enterprise.Application.Features.Providers;
using MediatR;

namespace Enterprise.Application.Features.Providers.Commands.DeleteAdminProviderImage;

public sealed record DeleteAdminProviderImageCommand(Guid Id) : IRequest<ProviderDto>;
