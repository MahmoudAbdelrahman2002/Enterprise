using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Admin.Users.Commands.UpdateAdminUser;

public sealed class UpdateAdminUserCommandHandler(
    IStaffManagerService staffManagerService)
    : IRequestHandler<UpdateAdminUserCommand, StaffDetailDto>
{
    public async Task<StaffDetailDto> Handle(
        UpdateAdminUserCommand request, CancellationToken cancellationToken)
    {
        var result = await staffManagerService.UpdateStaffAsync(
            request.Id,
            new UpdateStaffRequest(
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.RoleId),
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

        var updated = await staffManagerService.GetStaffByIdAsync(
            request.Id,
            UserType.Admin,
            providerId: null,
            cancellationToken);

        return updated!;
    }
}
