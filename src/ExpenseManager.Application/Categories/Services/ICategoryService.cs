using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Application.Categories.Models;
using ExpenseManager.Application.Categories.Requests;

namespace ExpenseManager.Application.Categories.Services;

public interface ICategoryService
{
    Task<IReadOnlyCollection<CategoryItem>> GetCategoriesAsync(Guid? userId = null, CancellationToken cancellationToken = default);

    Task<Guid> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);

    Task UpdateCategoryAsync(UpdateCategoryRequest request, CancellationToken cancellationToken = default);

    Task DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
