using System;

namespace ExpenseManager.Application.Categories.Requests;

public sealed record UpdateCategoryRequest(Guid CategoryId, string Name, string? Description);
