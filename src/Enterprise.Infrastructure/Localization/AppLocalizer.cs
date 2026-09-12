using Enterprise.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Enterprise.Infrastructure.Localization;

public sealed class AppLocalizer(IStringLocalizer<Messages> localizer) : Application.Common.Interfaces.IAppLocalizer
{
    public string this[string key] => localizer[key];

    public string this[string key, params object[] args] => localizer[key, args];
}
