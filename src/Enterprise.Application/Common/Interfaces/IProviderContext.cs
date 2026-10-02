using Enterprise.Application.Common.Exceptions;
using Enterprise.Domain.Entities;

namespace Enterprise.Application.Common.Interfaces;

/// <summary>
/// Resolves the current authenticated user's provider tenant id (claim first, then owner lookup).
/// </summary>
public interface IProviderContext
{
    Task<Guid> GetProviderIdAsync(CancellationToken cancellationToken = default);
}
