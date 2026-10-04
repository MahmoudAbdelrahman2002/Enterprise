using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Validation;

namespace Enterprise.Application.Features.Client.Orders.Queries.GetClientOrdersPage;

public sealed class GetClientOrdersPageQueryValidator(IAppLocalizer localizer) : PaginationRules<GetClientOrdersPageQuery>(localizer);
