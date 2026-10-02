using MediatR;

namespace Enterprise.Application.Features.Provider.Categories.Commands.DeleteCategory;

/// <param name="Id">Category id.</param>
/// <param name="DeleteRelatedProducts">
/// When false and the category has products, deletion is rejected (HTTP 409) so the client can confirm.
/// When true, related products are soft-deleted with the category.
/// </param>
public sealed record DeleteCategoryCommand(Guid Id, bool DeleteRelatedProducts = false) : IRequest;
