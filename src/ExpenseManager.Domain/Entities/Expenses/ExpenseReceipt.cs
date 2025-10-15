using System;
using ExpenseManager.Domain.Abstractions;

namespace ExpenseManager.Domain.Entities.Expenses;

public sealed class ExpenseReceipt : Entity
{
    public Guid ExpenseId { get; private set; }
    public string FileName { get; private set; }
    public string FilePath { get; private set; }
    public long FileSizeInBytes { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private ExpenseReceipt()
    {
        FileName = string.Empty;
        FilePath = string.Empty;
    }

    private ExpenseReceipt(Guid id, Guid expenseId, string fileName, string filePath, long fileSizeInBytes) : base(id)
    {
        ExpenseId = expenseId;
        FileName = fileName;
        FilePath = filePath;
        FileSizeInBytes = fileSizeInBytes;
        UploadedAt = DateTime.UtcNow;
    }

    public static ExpenseReceipt Create(Guid expenseId, string fileName, string filePath, long fileSizeInBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));
        }

        if (fileSizeInBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileSizeInBytes), fileSizeInBytes, "File size must be greater than zero.");
        }

        return new ExpenseReceipt(Guid.NewGuid(), expenseId, fileName.Trim(), filePath.Trim(), fileSizeInBytes);
    }
}