using Enterprise.Application.Features.Providers;
using MediatR;

namespace Enterprise.Application.Features.Providers.Commands.UpdateProvider;

public sealed record UpdateProviderCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string CompanyName,
    string? PhoneNumber) : IRequest<ProviderDto>;
