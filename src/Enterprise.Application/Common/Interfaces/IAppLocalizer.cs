namespace Enterprise.Application.Common.Interfaces;

/// <summary>
/// Resolves a resource key to the current request culture (en / it / ar).
/// </summary>
public interface IAppLocalizer
{
    string this[string key] { get; }
    string this[string key, params object[] args] { get; }
}
