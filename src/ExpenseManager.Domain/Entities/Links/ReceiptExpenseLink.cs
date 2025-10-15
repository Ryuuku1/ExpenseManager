using ExpenseManager.Domain.Abstractions;
using ExpenseManager.Domain.ValueObjects;

namespace ExpenseManager.Domain.Entities.Links;

public sealed class ReceiptExpenseLink : Entity
{
    public Guid ExpenseId { get; private set; }
    public Guid ReceiptId { get; private set; }
    public decimal? AllocationPercent { get; private set; }
    public Money? AllocationAmount { get; private set; }
    public string? Notes { get; private set; }
    public AuditTrail AuditTrail { get; private set; }

    private ReceiptExpenseLink()
    {
        AuditTrail = AuditTrail.Create(Guid.Empty, DateTime.UtcNow);
    }

    private ReceiptExpenseLink(Guid id, Guid expenseId, Guid receiptId, decimal? allocationPercent, Money? allocationAmount, string? notes, AuditTrail auditTrail) : base(id)
    {
        ExpenseId = expenseId;
        ReceiptId = receiptId;
        AllocationPercent = allocationPercent;
        AllocationAmount = allocationAmount;
        Notes = notes;
        AuditTrail = auditTrail;
    }

    public static ReceiptExpenseLink Create(Guid expenseId, Guid receiptId, decimal? allocationPercent, Money? allocationAmount, string? notes, AuditTrail auditTrail)
    {
        ValidateAllocation(allocationPercent);

        if (auditTrail is null)
        {
            throw new ArgumentNullException(nameof(auditTrail));
        }

        return new ReceiptExpenseLink(Guid.NewGuid(), expenseId, receiptId, allocationPercent, allocationAmount, notes?.Trim(), auditTrail);
    }

    public void Update(decimal? allocationPercent, Money? allocationAmount, string? notes, AuditTrail auditTrail)
    {
        ValidateAllocation(allocationPercent);

        if (auditTrail is null)
        {
            throw new ArgumentNullException(nameof(auditTrail));
        }

        AllocationPercent = allocationPercent;
        AllocationAmount = allocationAmount;
        Notes = notes?.Trim();
        AuditTrail = auditTrail;
    }

    public Money CalculateAllocatedAmount(Money receiptNetAmount)
    {
        if (AllocationAmount is not null)
        {
            if (AllocationAmount.Currency != receiptNetAmount.Currency)
            {
                throw new InvalidOperationException("Allocation amount currency must match receipt currency.");
            }

            return AllocationAmount;
        }

        if (AllocationPercent is not null)
        {
            return receiptNetAmount.MultiplyByPercent(AllocationPercent.Value);
        }

        return Money.Zero(receiptNetAmount.Currency);
    }

    private static void ValidateAllocation(decimal? allocationPercent)
    {
        if (allocationPercent is null)
        {
            return;
        }

        if (allocationPercent < 0 || allocationPercent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(allocationPercent), allocationPercent, "Allocation percentage must be between 0 and 100.");
        }
    }
}
