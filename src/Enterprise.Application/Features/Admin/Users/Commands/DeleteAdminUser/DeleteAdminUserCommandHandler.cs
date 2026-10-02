using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Admin.Users.Commands.DeleteAdminUser;

public sealed class DeleteAdminUserCommandHandler(
    IStaffManagerService staffManagerService,
    ILogger<DeleteAdminUserCommandHandler> logger)
    : IRequestHandler<DeleteAdminUserCommand>
{
    public async Task Handle(DeleteAdminUserCommand request, CancellationToken cancellationToken)
    {
        var result = await staffManagerService.DeleteStaffAsync(
            request.Id,
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

        logger.LogInformation("Deleted admin user {UserId}", request.Id);
    }
}
