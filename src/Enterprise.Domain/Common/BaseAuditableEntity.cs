namespace Enterprise.Domain.Common;

/// <summary>
/// Adds "who/when" bookkeeping to an entity. Populated automatically by
/// <c>AuditableEntitySaveChangesInterceptor</c> in the Infrastructure layer,
/// never set manually by application code, so every write path stays consistent.
/// </summary>
public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }
}
