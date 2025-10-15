using System;

namespace ExpenseManager.Application.Categories.Models;

public sealed record CategoryItem(
    Guid Id,
    string Name,
    string? Description,
    bool IsDefault,
    int ExpenseCount);
