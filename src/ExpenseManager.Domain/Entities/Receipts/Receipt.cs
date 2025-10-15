using ExpenseManager.Domain.Abstractions;
using ExpenseManager.Domain.Entities.Links;
using ExpenseManager.Domain.Enumerations;
using ExpenseManager.Domain.ValueObjects;

namespace ExpenseManager.Domain.Entities.Receipts;

public sealed class Receipt : AggregateRoot
{
    private readonly List<ReceiptExpenseLink> _expenseLinks = new();

    public Guid UserId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string? Vendor { get; private set; }
    public DateOnly ReceiptDate { get; private set; }
    public Money NetAmount { get; private set; }
    public decimal? CommissionPercent { get; private set; }
    public string? FileName { get; private set; }
    public string? FilePath { get; private set; }
    public long? FileSizeInBytes { get; private set; }
    public DateTime CapturedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<ReceiptExpenseLink> ExpenseLinks => _expenseLinks.AsReadOnly();

#pragma warning disable IDE0051 // Required by Entity Framework
    private Receipt()
    {
        Title = string.Empty;
        NetAmount = Money.Create(0, Currency.Eur);
    }
#pragma warning restore IDE0051

    private Receipt(
        Guid id,
        Guid userId,
        string title,
        Money netAmount,
        DateOnly receiptDate,
        Guid? categoryId,
        string? description,
        string? referenceNumber,
        string? vendor,
        decimal? commissionPercent,
        string? fileName,
        string? filePath,
        long? fileSizeInBytes) : base(id)
    {
        UserId = userId;
        Title = title;
        NetAmount = netAmount;
        ReceiptDate = receiptDate;
        CategoryId = categoryId;
        Description = description;
        ReferenceNumber = referenceNumber;
        Vendor = vendor;
        CommissionPercent = commissionPercent;
        FileName = fileName;
        FilePath = filePath;
        FileSizeInBytes = fileSizeInBytes;
        CapturedAt = DateTime.UtcNow;
        UpdatedAt = CapturedAt;
    }

    public static Receipt Create(
        Guid userId,
        string title,
        Money netAmount,
        DateOnly receiptDate,
        Guid? categoryId = null,
        string? description = null,
        string? referenceNumber = null,
        string? vendor = null,
        decimal? commissionPercent = null,
        string? fileName = null,
        string? filePath = null,
        long? fileSizeInBytes = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        }

        ValidateCommission(commissionPercent);

        if (fileSizeInBytes is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileSizeInBytes), fileSizeInBytes, "File size cannot be negative.");
        }

        return new Receipt(
            Guid.NewGuid(),
            userId,
            title.Trim(),
            netAmount,
            receiptDate,
            categoryId,
            description?.Trim(),
            referenceNumber?.Trim(),
            vendor?.Trim(),
            commissionPercent,
            fileName?.Trim(),
            filePath?.Trim(),
            fileSizeInBytes);
    }

    public void UpdateDetails(
        string title,
        Money netAmount,
        DateOnly receiptDate,
        Guid? categoryId,
        string? description,
        string? referenceNumber,
        string? vendor,
        decimal? commissionPercent)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        }

        ValidateCommission(commissionPercent);

        Title = title.Trim();
        NetAmount = netAmount;
        ReceiptDate = receiptDate;
        CategoryId = categoryId;
        Description = description?.Trim();
        ReferenceNumber = referenceNumber?.Trim();
        Vendor = vendor?.Trim();
        CommissionPercent = commissionPercent;
        Touch();
    }

    public void UpdateFileMetadata(string? fileName, string? filePath, long? fileSizeInBytes)
    {
        if (fileSizeInBytes is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileSizeInBytes), fileSizeInBytes, "File size cannot be negative.");
        }

        FileName = fileName?.Trim();
        FilePath = filePath?.Trim();
        FileSizeInBytes = fileSizeInBytes;
        Touch();
    }

    public void AttachLink(ReceiptExpenseLink link)
    {
        if (link is null)
        {
            throw new ArgumentNullException(nameof(link));
        }

        if (link.ReceiptId != Id)
        {
            throw new InvalidOperationException("Link receipt identifier does not match receipt instance.");
        }

        if (_expenseLinks.Exists(existing => existing.Id == link.Id))
        {
            return;
        }

        _expenseLinks.Add(link);
        Touch();
    }

    public void DetachLink(Guid linkId)
    {
        var removed = _expenseLinks.RemoveAll(link => link.Id == linkId);
        if (removed > 0)
        {
            Touch();
        }
    }

    public Money GetAllocatedTotal()
    {
        var total = Money.Zero(NetAmount.Currency);
        foreach (var link in _expenseLinks)
        {
            total = total.Add(link.CalculateAllocatedAmount(NetAmount));
        }

        return total;
    }

    public Money GetUnallocatedNet()
    {
        var allocated = GetAllocatedTotal();
        var remaining = NetAmount.Amount - allocated.Amount;

        if (remaining <= 0)
        {
            return Money.Zero(NetAmount.Currency);
        }

        return Money.Create(remaining, NetAmount.Currency);
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateCommission(decimal? commissionPercent)
    {
        if (!commissionPercent.HasValue)
        {
            return;
        }

        if (commissionPercent < 0 || commissionPercent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(commissionPercent), commissionPercent, "Commission percent must be between 0 and 100.");
        }
    }
}
