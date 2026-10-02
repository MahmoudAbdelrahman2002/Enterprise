using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Providers;
using MediatR;

namespace Enterprise.Application.Features.Providers.Commands.UploadAdminProviderImage;

public sealed record UploadAdminProviderImageCommand(Guid Id, ImageUploadFile File)
    : IRequest<ProviderDto>;
