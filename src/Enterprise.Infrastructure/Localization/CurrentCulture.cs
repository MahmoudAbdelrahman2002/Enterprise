using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Common;

namespace Enterprise.Infrastructure.Localization;

public sealed class CurrentCulture : ICurrentCulture
{
    public string LanguageCode => SupportedLanguages.Current;
}
