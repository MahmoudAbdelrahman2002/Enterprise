using MediatR;

namespace Enterprise.Application.Features.Admin.Users.Commands.DeleteAdminUser;

public sealed record DeleteAdminUserCommand(Guid Id) : IRequest;
