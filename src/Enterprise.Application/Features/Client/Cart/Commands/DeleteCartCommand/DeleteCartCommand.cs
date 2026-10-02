using MediatR;

namespace Enterprise.Application.Features.Client.Cart.Commands.DeleteCartCommand;

public class DeleteCartCommand(Guid providerId) : IRequest
{
    public Guid ProviderId { get; init; } = providerId;

}
