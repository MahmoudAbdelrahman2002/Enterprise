using Enterprise.Application.Features.Providers;
using MediatR;

namespace Enterprise.Application.Features.Provider.Commands.DeleteProviderImage;

public sealed record DeleteProviderImageCommand : IRequest<ProviderDto>;
