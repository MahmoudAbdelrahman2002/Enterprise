namespace Enterprise.Application.Common.Behaviors;

/// <summary>
/// Opt-in marker for commands: on successful completion,
/// <see cref="CacheInvalidationBehavior{TRequest,TResponse}"/> removes every cache entry whose
/// key starts with one of <see cref="CacheKeyPrefixesToInvalidate"/>. Keeps the "who
/// invalidates what" decision next to the write that causes it, instead of scattering
/// <c>_cache.Remove(...)</c> calls across handlers.
/// </summary>
public interface ICacheInvalidatorCommand
{
    IReadOnlyCollection<string> CacheKeyPrefixesToInvalidate { get; }
}
