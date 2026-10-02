using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Admin.Clients.Commands.SetAdminClientActive;

public sealed class SetAdminClientActiveCommandHandler(IStaffManagerService staffManagerService)
    : IRequestHandler<SetAdminClientActiveCommand>
{
    public async Task Handle(SetAdminClientActiveCommand request, CancellationToken cancellationToken)
    {
        var result = await staffManagerService.SetStaffActiveAsync(
            request.Id,
            request.IsActive,
            UserType.Client,
            providerId: null,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.Error == MessageKeys.Error.NotFound)
            {
                throw NotFoundException.For("Client", request.Id);
            }

            throw new ConflictException(result.Error ?? MessageKeys.Error.Conflict, result.Errors);
        }
    }
}
