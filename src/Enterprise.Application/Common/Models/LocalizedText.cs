namespace Enterprise.Application.Common.Models;

/// <summary>
/// Translatable catalog text. English is required; Italian and Arabic are optional
/// and fall back to English when omitted.
/// JSON: <c>{ "en": "...", "it": "...", "ar": "..." }</c>
/// </summary>
public sealed record LocalizedText(string En, string? It = null, string? Ar = null);
