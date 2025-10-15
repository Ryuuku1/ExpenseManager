using ExpenseManager.Domain.Abstractions;
using ExpenseManager.Domain.Entities.Links;
using ExpenseManager.Domain.Enumerations;
using ExpenseManager.Domain.ValueObjects;

namespace ExpenseManager.Domain.Entities.Expenses;

public sealed class Expense : AggregateRoot
{
    private readonly List<ExpenseReceipt> _receipts = new();
    private readonly List<ReceiptExpenseLink> _receiptLinks = new();

    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public Money Amount { get; private set; }
    public ExpenseStatus Status { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public DateOnly ExpenseDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public RecurrenceType Recurrence { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<ExpenseReceipt> Receipts => _receipts.AsReadOnly();
    public IReadOnlyCollection<ReceiptExpenseLink> ReceiptLinks => _receiptLinks.AsReadOnly();

#pragma warning disable IDE0051 // Required by Entity Framework
    private Expense()
    {
        Title = string.Empty;
        Amount = Money.Create(0, Currency.Eur);
    }
#pragma warning restore IDE0051

    private Expense(Guid id, Guid userId, Guid categoryId, string title, string? description, Money amount, ExpenseStatus status, PaymentMethod paymentMethod, DateOnly expenseDate, DateOnly? dueDate, RecurrenceType recurrence) : base(id)
    {
        UserId = userId;
        CategoryId = categoryId;
        Title = title;
        Description = description;
        Amount = amount;
        Status = status;
        PaymentMethod = paymentMethod;
        ExpenseDate = expenseDate;
        DueDate = dueDate;
        Recurrence = recurrence;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Expense Create(Guid userId, Guid categoryId, string title, Money amount, DateOnly expenseDate, PaymentMethod paymentMethod, ExpenseStatus status = ExpenseStatus.Approved, string? description = null, DateOnly? dueDate = null, RecurrenceType recurrence = RecurrenceType.None)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        }

        return new Expense(Guid.NewGuid(), userId, categoryId, title.Trim(), description?.Trim(), amount, status, paymentMethod, expenseDate, dueDate, recurrence);
    }

    public void UpdateDetails(string title, string? description, Guid categoryId, Money amount, PaymentMethod paymentMethod, DateOnly expenseDate, DateOnly? dueDate, RecurrenceType recurrence)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        }

        Title = title.Trim();
        Description = description?.Trim();
        CategoryId = categoryId;
        Amount = amount;
        PaymentMethod = paymentMethod;
        ExpenseDate = expenseDate;
        DueDate = dueDate;
        Recurrence = recurrence;
        Touch();
    }

    public void ChangeStatus(ExpenseStatus status)
    {
        Status = status;
        Touch();
    }

    public void AddReceipt(ExpenseReceipt receipt)
    {
        _receipts.Add(receipt);
        Touch();
    }

    public void RemoveReceipt(Guid receiptId)
    {
        _receipts.RemoveAll(r => r.Id == receiptId);
        Touch();
    }

    public void AttachReceiptLink(ReceiptExpenseLink link)
    {
        if (link is null)
        {
            throw new ArgumentNullException(nameof(link));
        }

        if (link.ExpenseId != Id)
        {
            throw new InvalidOperationException("Link expense identifier does not match expense instance.");
        }

        if (_receiptLinks.Any(existing => existing.Id == link.Id))
        {
            return;
        }

        _receiptLinks.Add(link);
        Touch();
    }

    public void DetachReceiptLink(Guid linkId)
    {
        var removed = _receiptLinks.RemoveAll(link => link.Id == linkId);
        if (removed > 0)
        {
            Touch();
        }
    }

    public Money CalculateTotalAllocated(Func<Guid, Money> receiptNetAmountResolver)
    {
        if (receiptNetAmountResolver is null)
        {
            throw new ArgumentNullException(nameof(receiptNetAmountResolver));
        }

        var total = Money.Zero(Amount.Currency);

        foreach (var link in _receiptLinks)
        {
            var receiptNet = receiptNetAmountResolver(link.ReceiptId);
            var allocation = link.CalculateAllocatedAmount(receiptNet);

            if (allocation.Currency != Amount.Currency)
            {
                throw new InvalidOperationException("Allocation currency must match expense currency.");
            }

            total = total.Add(allocation);
        }

        return total;
    }

    public Money CalculateRemainingAmount(Func<Guid, Money> receiptNetAmountResolver)
    {
        var allocated = CalculateTotalAllocated(receiptNetAmountResolver);
        if (allocated.Amount >= Amount.Amount)
        {
            return Money.Zero(Amount.Currency);
        }

        var remaining = Amount.Amount - allocated.Amount;
        return Money.Create(remaining, Amount.Currency);
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}