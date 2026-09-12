namespace Enterprise.Application.Common.Interfaces;

/// <summary>
/// Normalized request language: <c>en</c>, <c>it</c>, or <c>ar</c>.
/// </summary>
public interface ICurrentCulture
{
    string LanguageCode { get; }
}
