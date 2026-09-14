using Enterprise.Application.Features.Providers;
using MediatR;

namespace Enterprise.Application.Features.Providers.Commands.CreateProvider;

public sealed record CreateProviderCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string CompanyName,
    string? PhoneNumber) : IRequest<ProviderDto>;
