using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;

namespace Enterprise.Application.Features.Admin.Users.Commands.CreateAdminUser;

public sealed class CreateAdminUserCommandHandler(
    IStaffManagerService staffManagerService)
    : IRequestHandler<CreateAdminUserCommand, StaffDetailDto>
{
    public async Task<StaffDetailDto> Handle(
        CreateAdminUserCommand request, CancellationToken cancellationToken)
    {
        var result = await staffManagerService.CreateStaffAsync(
            new CreateStaffRequest(
                request.FirstName,
                request.LastName,
                request.Email,
                request.PhoneNumber,
                request.Password,
                request.RoleId),
            UserType.Admin,
            providerId: null,
            cancellationToken);

        if (!result.Succeeded)
        {
            throw new ConflictException(result.Error ?? MessageKeys.Error.Conflict, result.Errors);
        }

        var created = await staffManagerService.GetStaffByIdAsync(
            result.StaffId!.Value,
            UserType.Admin,
            providerId: null,
            cancellationToken);

        return created!;
    }
}
