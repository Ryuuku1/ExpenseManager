using ExpenseManager.Domain.Abstractions;

namespace ExpenseManager.Domain.ValueObjects;

public sealed class AuditTrail : ValueObject
{
    public Guid CreatedBy { get; }
    public DateTime CreatedAt { get; }
    public Guid? UpdatedBy { get; }
    public DateTime? UpdatedAt { get; }

    private AuditTrail(Guid createdBy, DateTime createdAt, Guid? updatedBy, DateTime? updatedAt)
    {
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }

    public static AuditTrail Create(Guid createdBy, DateTime createdAt)
    {
        return new AuditTrail(createdBy, createdAt, null, null);
    }

    public AuditTrail Update(Guid updatedBy, DateTime updatedAt)
    {
        if (updatedAt < CreatedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(updatedAt), updatedAt, "Updated timestamp cannot be earlier than the creation timestamp.");
        }

        return new AuditTrail(CreatedBy, CreatedAt, updatedBy, updatedAt);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CreatedBy;
        yield return CreatedAt;
        yield return UpdatedBy;
        yield return UpdatedAt;
    }
}
