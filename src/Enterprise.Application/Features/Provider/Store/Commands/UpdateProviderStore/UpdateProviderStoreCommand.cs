using MediatR;

namespace Enterprise.Application.Features.Provider.Store.Commands.UpdateProviderStore;

public sealed record UpdateProviderStoreCommand(string CompanyName, string? PhoneNumber) : IRequest<ProviderStoreDto>;
