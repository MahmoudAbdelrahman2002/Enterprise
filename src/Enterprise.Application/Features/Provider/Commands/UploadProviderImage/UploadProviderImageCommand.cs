using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Providers;
using MediatR;

namespace Enterprise.Application.Features.Provider.Commands.UploadProviderImage;

public sealed record UploadProviderImageCommand(ImageUploadFile File) : IRequest<ProviderDto>;
