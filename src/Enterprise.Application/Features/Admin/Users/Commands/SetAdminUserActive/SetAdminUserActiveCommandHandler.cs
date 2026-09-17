using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Admin.Users.Commands.SetAdminUserActive;

public sealed class SetAdminUserActiveCommandHandler(
    IStaffManagerService staffManagerService)
    : IRequestHandler<SetAdminUserActiveCommand>
{
    public async Task Handle(SetAdminUserActiveCommand request, CancellationToken cancellationToken)
    {
        var result = await staffManagerService.SetStaffActiveAsync(
            request.Id,
            request.IsActive,
            UserType.Admin,
            providerId: null,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.Error == MessageKeys.Error.NotFound)
            {
                throw NotFoundException.For("AdminUser", request.Id);
            }

            throw new ConflictException(result.Error ?? MessageKeys.Error.Conflict, result.Errors);
        }
    }
}
