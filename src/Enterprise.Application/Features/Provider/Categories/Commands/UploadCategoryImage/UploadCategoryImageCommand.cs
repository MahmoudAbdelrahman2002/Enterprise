using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Provider.Categories.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Provider.Categories.Commands.UploadCategoryImage;

public sealed record UploadCategoryImageCommand(Guid Id, ImageUploadFile File)
    : IRequest<CategoryDetailDto>;
