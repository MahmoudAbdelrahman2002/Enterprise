using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Validation;

namespace Enterprise.Application.Features.Notifications.Queries.GetNotificationsPage;

public sealed class GetNotificationsPageQueryValidator(IAppLocalizer localizer) : PaginationRules<GetNotificationsPageQuery>(localizer);
