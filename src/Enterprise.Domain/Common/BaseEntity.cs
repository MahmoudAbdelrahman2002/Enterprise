namespace Enterprise.Domain.Common;

/// <summary>
/// Root identity contract for every aggregate/entity in the model.
/// Using a Guid (v7-friendly) primary key instead of an int identity avoids
/// leaking sequential database identifiers through the API and lets callers
/// (or the database, via SEQUENTIALID/newsequentialid()) generate ids without a round-trip.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
