namespace Enterprise.Domain.Common;

/// <summary>
/// Marker + state contract for entities that are logically rather than physically deleted.
/// The <c>SaveChangesInterceptor</c> intercepts EF's <c>EntityState.Deleted</c> for anything
/// implementing this interface and rewrites it to an update, and the DbContext applies a global
/// query filter so soft-deleted rows disappear from every LINQ query without extra ceremony
/// at each call site.
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
    string? DeletedBy { get; set; }
}
