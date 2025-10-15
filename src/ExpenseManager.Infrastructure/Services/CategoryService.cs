using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseManager.Application.Categories.Models;
using ExpenseManager.Application.Categories.Requests;
using ExpenseManager.Application.Categories.Services;
using ExpenseManager.Domain.Entities.Categories;
using ExpenseManager.Domain.Entities.Expenses;
using ExpenseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Infrastructure.Services;

internal sealed class CategoryService : ICategoryService
{
    private readonly ExpenseManagerDbContext _dbContext;

    public CategoryService(ExpenseManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<CategoryItem>> GetCategoriesAsync(Guid? userId = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Expense> expenses = _dbContext.Expenses.AsNoTracking();
        if (userId is Guid id)
        {
            expenses = expenses.Where(expense => expense.UserId == id);
        }

        return await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryItem(
                category.Id,
                category.Name,
                category.Description,
                category.IsDefault,
                expenses.Count(expense => expense.CategoryId == category.Id)))
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Categories.AnyAsync(category => category.Name == request.Name, cancellationToken))
        {
            throw new InvalidOperationException($"Category '{request.Name}' already exists.");
        }

        var category = Category.Create(request.Name, request.Description);
        await _dbContext.Categories.AddAsync(category, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return category.Id;
    }

    public async Task UpdateCategoryAsync(UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories.FirstOrDefaultAsync(category => category.Id == request.CategoryId, cancellationToken);

        if (category is null)
        {
            throw new InvalidOperationException("Category not found.");
        }

        category.UpdateDetails(request.Name, request.Description);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories.FirstOrDefaultAsync(category => category.Id == categoryId, cancellationToken);

        if (category is null)
        {
            return;
        }

        if (category.IsDefault)
        {
            throw new InvalidOperationException("Cannot delete a default category.");
        }

        var inUse = await _dbContext.Expenses.AnyAsync(expense => expense.CategoryId == categoryId, cancellationToken);
        if (inUse)
        {
            throw new InvalidOperationException("Cannot delete a category that has associated expenses.");
        }

        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
