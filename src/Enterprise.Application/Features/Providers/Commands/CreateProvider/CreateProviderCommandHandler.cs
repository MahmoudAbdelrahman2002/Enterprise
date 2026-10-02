using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Localization;
using Enterprise.Application.Features.Notifications;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Enums;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Providers.Commands.CreateProvider;

public sealed class CreateProviderCommandHandler(
    IUserAccountService userAccountService,
    IUnitOfWork unitOfWork,
    IProviderAdminQueryService providerAdminQueryService,
    INotificationService notificationService,
    ICurrentUserService currentUserService,
    ILogger<CreateProviderCommandHandler> logger) : IRequestHandler<CreateProviderCommand, ProviderDto>
{
    public async Task<ProviderDto> Handle(CreateProviderCommand request, CancellationToken cancellationToken)
    {
        if (request.ServiceId.HasValue)
        {
            var service = await unitOfWork.Services.GetByIdAsync(request.ServiceId.Value, cancellationToken);
            if (service is null || !service.IsActive)
            {
                throw NotFoundException.For(nameof(MarketplaceService), request.ServiceId.Value);
            }
        }

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

            throw new ConflictException(createResult.Error ?? MessageKeys.Provider.UnableToCreate, createResult.Errors);
        }

        var userId = createResult.UserId.Value;
        try
        {
            var provider = new Domain.Entities.Provider(
                userId,
                request.CompanyName.Trim(),
                NormalizePhone(request.PhoneNumber),
                request.ServiceId);

            unitOfWork.Providers.Add(provider);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await NotifyAdminsOfNewProviderAsync(provider, cancellationToken);

            var detail = await providerAdminQueryService.GetByIdAsync(provider.Id, cancellationToken)
                ?? throw new ConflictException(MessageKeys.Provider.UnableToCreate);

            logger.LogInformation("Created provider {ProviderId} for {Email}", provider.Id, request.Email);
            return detail.ToDto();
        }
        catch
        {
            await userAccountService.DeleteByEmailAsync(request.Email, cancellationToken);
            throw;
        }
    }

    private async Task NotifyAdminsOfNewProviderAsync(
        Domain.Entities.Provider provider,
        CancellationToken cancellationToken)
    {
        try
        {
            var adminIds = await userAccountService.GetActiveUserIdsByTypeAsync(
                UserType.Admin,
                cancellationToken);

            var recipients = adminIds
                .Where(id => id != currentUserService.UserId)
                .ToList();

            if (recipients.Count == 0)
            {
                return;
            }

            await notificationService.NotifyManyAsync(
                recipients,
                UserType.Admin,
                "New provider registered",
                $"\"{provider.CompanyName}\" was added to the marketplace.",
                NotificationTypes.NewProviderRegistration,
                provider.Id,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Provider {ProviderId} was created but admin notifications failed",
                provider.Id);
        }
    }

    private static string? NormalizePhone(string? phone) =>
        string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
}
