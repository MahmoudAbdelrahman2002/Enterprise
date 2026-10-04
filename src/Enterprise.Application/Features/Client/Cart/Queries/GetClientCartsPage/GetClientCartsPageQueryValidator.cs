using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Validation;

namespace Enterprise.Application.Features.Client.Cart.Queries.GetClientCartsPage;

public sealed class GetClientCartsPageQueryValidator(IAppLocalizer localizer) : PaginationRules<GetClientCartsPageQuery>(localizer);
