using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Providers.Commands.CreateProvider;

public sealed class CreateProviderCommandHandler(
    IUserAccountService userAccountService,
    IUnitOfWork unitOfWork,
    IProviderAdminQueryService providerAdminQueryService) : IRequestHandler<CreateProviderCommand, ProviderDto>
{
    public async Task<ProviderDto> Handle(CreateProviderCommand request, CancellationToken cancellationToken)
    {
        var createResult = await userAccountService.CreateProviderAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            cancellationToken);

        if (!createResult.Succeeded || createResult.UserId is null)
        {
            if (createResult.Error == MessageKeys.Account.EmailExists)
            {
                throw new ConflictException(MessageKeys.Account.EmailExists);
            }

            throw new ConflictException(createResult.Error ?? MessageKeys.Provider.UnableToCreate);
        }

        var userId = createResult.UserId.Value;
        try
        {
            var provider = new Domain.Entities.Provider(userId, request.CompanyName.Trim(), NormalizePhone(request.PhoneNumber));
            unitOfWork.Providers.Add(provider);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var detail = await providerAdminQueryService.GetByIdAsync(provider.Id, cancellationToken)
                ?? throw new ConflictException(MessageKeys.Provider.UnableToCreate);

            return detail.ToDto();
        }
        catch
        {
            await userAccountService.DeleteByEmailAsync(request.Email, cancellationToken);
            throw;
        }
    }

    private static string? NormalizePhone(string? phone) =>
        string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
}
