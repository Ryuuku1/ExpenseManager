using System;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Application.Reports.Models;

public sealed record SpendingByCategoryReportItem(
    Guid CategoryId,
    string CategoryName,
    decimal TotalAmount,
    Currency Currency,
    int ExpenseCount);
