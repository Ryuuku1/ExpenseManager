using ExpenseManager.Application.Expenses.Services;
using ExpenseManager.Application.ReceiptExpenseLinks.Repositories;
using ExpenseManager.Application.ReceiptExpenseLinks.Requests;
using ExpenseManager.Application.ReceiptExpenseLinks.Responses;
using ExpenseManager.Application.ReceiptExpenseLinks.Services;
using ExpenseManager.Application.Receipts.Services;
using ExpenseManager.Domain.Entities.Links;
using ExpenseManager.Domain.Entities.Expenses;
using ExpenseManager.Domain.Entities.Receipts;
using ExpenseManager.Domain.Enumerations;
using ExpenseManager.Domain.ValueObjects;
using ExpenseManager.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Infrastructure.Services;

internal sealed class ReceiptExpenseLinkService : IReceiptExpenseLinkService
{
    private readonly ExpenseManagerDbContext _dbContext;
    private readonly IReceiptExpenseLinkReader _linkReader;
    private readonly IReceiptExpenseLinkWriter _linkWriter;
    private readonly IExpenseReader _expenseReader;
    private readonly IReceiptReader _receiptReader;
    private readonly IValidator<CreateReceiptExpenseLinkRequest> _createValidator;
    private readonly IValidator<UpdateReceiptExpenseLinkRequest> _updateValidator;
    private readonly IValidator<DeleteReceiptExpenseLinkRequest> _deleteValidator;
    private readonly IValidator<GetLinksByExpenseIdRequest> _byExpenseValidator;
    private readonly IValidator<GetLinksByReceiptIdRequest> _byReceiptValidator;
    private readonly IValidator<GetUnlinkedExpensesRequest> _unlinkedExpensesValidator;
    private readonly IValidator<GetUnlinkedReceiptsRequest> _unlinkedReceiptsValidator;
    private readonly IValidator<GetConnectionsSummaryRequest> _summaryValidator;

    public ReceiptExpenseLinkService(
        ExpenseManagerDbContext dbContext,
        IReceiptExpenseLinkReader linkReader,
        IReceiptExpenseLinkWriter linkWriter,
        IExpenseReader expenseReader,
        IReceiptReader receiptReader,
        IValidator<CreateReceiptExpenseLinkRequest> createValidator,
        IValidator<UpdateReceiptExpenseLinkRequest> updateValidator,
        IValidator<DeleteReceiptExpenseLinkRequest> deleteValidator,
        IValidator<GetLinksByExpenseIdRequest> byExpenseValidator,
        IValidator<GetLinksByReceiptIdRequest> byReceiptValidator,
        IValidator<GetUnlinkedExpensesRequest> unlinkedExpensesValidator,
        IValidator<GetUnlinkedReceiptsRequest> unlinkedReceiptsValidator,
        IValidator<GetConnectionsSummaryRequest> summaryValidator)
    {
        _dbContext = dbContext;
        _linkReader = linkReader;
        _linkWriter = linkWriter;
        _expenseReader = expenseReader;
        _receiptReader = receiptReader;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _deleteValidator = deleteValidator;
        _byExpenseValidator = byExpenseValidator;
        _byReceiptValidator = byReceiptValidator;
        _unlinkedExpensesValidator = unlinkedExpensesValidator;
        _unlinkedReceiptsValidator = unlinkedReceiptsValidator;
        _summaryValidator = summaryValidator;
    }

    public async Task<ReceiptExpenseLinkDetailResponse> CreateAsync(CreateReceiptExpenseLinkRequest request, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

    await LoadExpenseAsync(request.UserId, request.ExpenseId, cancellationToken).ConfigureAwait(false);
    await LoadReceiptAsync(request.UserId, request.ReceiptId, cancellationToken).ConfigureAwait(false);

        if (await _linkReader.ExistsAsync(request.ExpenseId, request.ReceiptId, cancellationToken))
        {
            throw new InvalidOperationException("A link between the specified expense and receipt already exists.");
        }

        var audit = AuditTrail.Create(request.RequestedBy, DateTime.UtcNow);
        var link = ReceiptExpenseLink.Create(request.ExpenseId, request.ReceiptId, null, null, request.Notes, audit);

        await _linkWriter.CreateAsync(link, cancellationToken);

        return await LoadLinkDetailAsync(link.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to load link after creation.");
    }

    public async Task<ReceiptExpenseLinkDetailResponse> UpdateAsync(UpdateReceiptExpenseLinkRequest request, CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var link = await _linkReader.GetAsync(request.LinkId, cancellationToken)
            ?? throw new InvalidOperationException("The requested link does not exist.");

        if (link.ExpenseId != request.ExpenseId || link.ReceiptId != request.ReceiptId)
        {
            throw new InvalidOperationException("Cannot change the associated expense or receipt for an existing link.");
        }

    await LoadExpenseAsync(request.UserId, request.ExpenseId, cancellationToken).ConfigureAwait(false);
    await LoadReceiptAsync(request.UserId, request.ReceiptId, cancellationToken).ConfigureAwait(false);

        var updatedAudit = link.AuditTrail.Update(request.RequestedBy, DateTime.UtcNow);
    link.Update(null, null, request.Notes, updatedAudit);

        await _linkWriter.UpdateAsync(link, cancellationToken);

        return await LoadLinkDetailAsync(link.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to load link after update.");
    }

    public async Task DeleteAsync(DeleteReceiptExpenseLinkRequest request, CancellationToken cancellationToken = default)
    {
        await _deleteValidator.ValidateAndThrowAsync(request, cancellationToken);

        var link = await _linkReader.GetAsync(request.LinkId, cancellationToken);
        if (link is null)
        {
            return;
        }

        var expense = await _expenseReader.GetAsync(request.UserId, link.ExpenseId, cancellationToken);
        if (expense is null)
        {
            throw new InvalidOperationException("Expense not found for the specified user.");
        }

        var receipt = await _receiptReader.GetAsync(request.UserId, link.ReceiptId, cancellationToken);
        if (receipt is null)
        {
            throw new InvalidOperationException("Receipt not found for the specified user.");
        }

        await _linkWriter.DeleteAsync(request.LinkId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ReceiptExpenseLinkDetailResponse>> GetByExpenseAsync(GetLinksByExpenseIdRequest request, CancellationToken cancellationToken = default)
    {
        await _byExpenseValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureExpenseExistsAsync(request.UserId, request.ExpenseId, cancellationToken);

        var expenseId = request.ExpenseId;
        var fromDate = request.From;
        var toDate = request.To;
        var receiptCategory = request.ReceiptCategoryId;

        var rows = await BuildDetailQuery(
                userFilter: request.UserId,
                expenseId: expenseId,
                receiptFrom: fromDate,
                receiptTo: toDate,
                receiptCategoryId: receiptCategory)
            .ToListAsync(cancellationToken);

        return rows
            .OrderByDescending(row => row.ReceiptDate)
            .ThenByDescending(row => row.CreatedAt)
            .Select(MapDetailRow)
            .ToList();
    }

    public async Task<IReadOnlyCollection<ReceiptExpenseLinkDetailResponse>> GetByReceiptAsync(GetLinksByReceiptIdRequest request, CancellationToken cancellationToken = default)
    {
        await _byReceiptValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureReceiptExistsAsync(request.UserId, request.ReceiptId, cancellationToken);

        var receiptId = request.ReceiptId;
        var fromDate = request.From;
        var toDate = request.To;
        var expenseCategory = request.ExpenseCategoryId;

        var rows = await BuildDetailQuery(
                userFilter: request.UserId,
                receiptId: receiptId,
                expenseFrom: fromDate,
                expenseTo: toDate,
                expenseCategoryId: expenseCategory)
            .ToListAsync(cancellationToken);

        return rows
            .OrderByDescending(row => row.ExpenseDate)
            .ThenByDescending(row => row.CreatedAt)
            .Select(MapDetailRow)
            .ToList();
    }

    public async Task<IReadOnlyCollection<LinkSummaryResponse>> GetUnlinkedExpensesAsync(GetUnlinkedExpensesRequest request, CancellationToken cancellationToken = default)
    {
        await _unlinkedExpensesValidator.ValidateAndThrowAsync(request, cancellationToken);

        var items = await _expenseReader.GetUnlinkedAsync(
            request.UserId,
            request.From,
            request.To,
            request.CategoryId,
            request.OnlyOpenExpenses,
            cancellationToken);

        return items
            .OrderByDescending(item => item.ExpenseDate)
            .ThenByDescending(item => item.ExpenseId)
            .Select(info => new LinkSummaryResponse(
                info.ExpenseId,
                info.ExpenseTitle,
                info.CategoryId,
                info.CategoryName,
                info.ExpenseDate,
                0,
                null))
            .ToList();
    }

    public async Task<IReadOnlyCollection<LinkSummaryResponse>> GetUnlinkedReceiptsAsync(GetUnlinkedReceiptsRequest request, CancellationToken cancellationToken = default)
    {
        await _unlinkedReceiptsValidator.ValidateAndThrowAsync(request, cancellationToken);

        var items = await _receiptReader.GetUnlinkedAsync(
            request.UserId,
            request.From,
            request.To,
            request.CategoryId,
            cancellationToken);

        return items
            .OrderByDescending(item => item.ReceiptDate)
            .ThenByDescending(item => item.ReceiptId)
            .Select(info => new LinkSummaryResponse(
                info.ReceiptId,
                info.ReceiptTitle,
                info.CategoryId,
                info.CategoryName,
                info.ReceiptDate,
                0,
                info.CommissionPercent))
            .ToList();
    }

    public async Task<ConnectionsSummaryResponse> GetSummaryAsync(GetConnectionsSummaryRequest request, CancellationToken cancellationToken = default)
    {
        await _summaryValidator.ValidateAndThrowAsync(request, cancellationToken);

        var expenseQuery = _dbContext.Expenses.AsNoTracking().Where(expense => expense.UserId == request.UserId);
        var receiptQuery = _dbContext.Receipts.AsNoTracking().Where(receipt => receipt.UserId == request.UserId);

        if (request.From.HasValue)
        {
            expenseQuery = expenseQuery.Where(expense => expense.ExpenseDate >= request.From.Value);
            receiptQuery = receiptQuery.Where(receipt => receipt.ReceiptDate >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            expenseQuery = expenseQuery.Where(expense => expense.ExpenseDate <= request.To.Value);
            receiptQuery = receiptQuery.Where(receipt => receipt.ReceiptDate <= request.To.Value);
        }

        if (request.ExpenseCategoryId.HasValue)
        {
            expenseQuery = expenseQuery.Where(expense => expense.CategoryId == request.ExpenseCategoryId.Value);
        }

        if (request.ReceiptCategoryId.HasValue)
        {
            receiptQuery = receiptQuery.Where(receipt => receipt.CategoryId == request.ReceiptCategoryId.Value);
        }

    var totalExpensesTask = expenseQuery.CountAsync(cancellationToken);
    var totalReceiptsTask = receiptQuery.CountAsync(cancellationToken);

        var linkRows = await BuildDetailQuery(
                userFilter: request.UserId,
                expenseFrom: request.From,
                expenseTo: request.To,
                expenseCategoryId: request.ExpenseCategoryId,
                receiptFrom: request.From,
                receiptTo: request.To,
                receiptCategoryId: request.ReceiptCategoryId)
            .ToListAsync(cancellationToken);

        var linkedExpenses = linkRows.Select(row => row.ExpenseId).Distinct().Count();
        var linkedReceipts = linkRows.Select(row => row.ReceiptId).Distinct().Count();
        var totalLinks = linkRows.Count;

        var totalExpenses = await totalExpensesTask;
        var totalReceipts = await totalReceiptsTask;

        var linkedExpenseCoverage = totalExpenses == 0 ? 0m : Math.Round((decimal)linkedExpenses / totalExpenses * 100m, 2, MidpointRounding.AwayFromZero);
        var linkedReceiptCoverage = totalReceipts == 0 ? 0m : Math.Round((decimal)linkedReceipts / totalReceipts * 100m, 2, MidpointRounding.AwayFromZero);

        return new ConnectionsSummaryResponse(
            totalExpenses,
            linkedExpenses,
            totalExpenses - linkedExpenses,
            linkedExpenseCoverage,
            totalReceipts,
            linkedReceipts,
            totalReceipts - linkedReceipts,
            linkedReceiptCoverage,
            totalLinks);
    }

    public async Task<IReadOnlyCollection<ReceiptExpenseLinkDetailResponse>> GetAllAsync(
        Guid userId,
        DateOnly? from,
        DateOnly? to,
        Guid? expenseCategoryId,
        Guid? receiptCategoryId,
        CancellationToken cancellationToken = default)
    {
        var rows = await BuildDetailQuery(
                userFilter: userId,
                expenseFrom: from,
                expenseTo: to,
                expenseCategoryId: expenseCategoryId,
                receiptFrom: from,
                receiptTo: to,
                receiptCategoryId: receiptCategoryId)
            .ToListAsync(cancellationToken);

        return rows
            .OrderByDescending(row => row.CreatedAt)
            .ThenByDescending(row => row.ExpenseDate)
            .ThenByDescending(row => row.ReceiptDate)
            .Select(MapDetailRow)
            .ToList();
    }

    private async Task<Expense> LoadExpenseAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken)
    {
        var expense = await _expenseReader.GetAsync(userId, expenseId, cancellationToken);
        return expense ?? throw new InvalidOperationException("Expense not found for the specified user.");
    }

    private async Task<Receipt> LoadReceiptAsync(Guid userId, Guid receiptId, CancellationToken cancellationToken)
    {
        var receipt = await _receiptReader.GetAsync(userId, receiptId, cancellationToken);
        return receipt ?? throw new InvalidOperationException("Receipt not found for the specified user.");
    }

    private async Task EnsureExpenseExistsAsync(Guid userId, Guid expenseId, CancellationToken cancellationToken)
    {
        var exists = await _expenseReader.GetAsync(userId, expenseId, cancellationToken);
        if (exists is null)
        {
            throw new InvalidOperationException("Expense not found for the specified user.");
        }
    }

    private async Task EnsureReceiptExistsAsync(Guid userId, Guid receiptId, CancellationToken cancellationToken)
    {
        var exists = await _receiptReader.GetAsync(userId, receiptId, cancellationToken);
        if (exists is null)
        {
            throw new InvalidOperationException("Receipt not found for the specified user.");
        }
    }

    private async Task<ReceiptExpenseLinkDetailResponse?> LoadLinkDetailAsync(Guid linkId, CancellationToken cancellationToken)
    {
        var row = await BuildDetailQuery(
                userFilter: null,
                linkId: linkId)
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : MapDetailRow(row);
    }

    private IQueryable<LinkDetailRowData> BuildDetailQuery(
        Guid? userFilter,
        Guid? linkId = null,
        Guid? expenseId = null,
        Guid? receiptId = null,
        DateOnly? expenseFrom = null,
        DateOnly? expenseTo = null,
        Guid? expenseCategoryId = null,
        DateOnly? receiptFrom = null,
        DateOnly? receiptTo = null,
        Guid? receiptCategoryId = null)
    {
        var links = _dbContext.ReceiptExpenseLinks.AsNoTracking();
        var expenses = _dbContext.Expenses.AsNoTracking();
        var receipts = _dbContext.Receipts.AsNoTracking();

        if (userFilter.HasValue)
        {
            expenses = expenses.Where(expense => expense.UserId == userFilter.Value);
            receipts = receipts.Where(receipt => receipt.UserId == userFilter.Value);
        }

        if (linkId.HasValue)
        {
            links = links.Where(link => link.Id == linkId.Value);
        }

        if (expenseId.HasValue)
        {
            links = links.Where(link => link.ExpenseId == expenseId.Value);
        }

        if (receiptId.HasValue)
        {
            links = links.Where(link => link.ReceiptId == receiptId.Value);
        }

        if (expenseFrom.HasValue)
        {
            expenses = expenses.Where(expense => expense.ExpenseDate >= expenseFrom.Value);
        }

        if (expenseTo.HasValue)
        {
            expenses = expenses.Where(expense => expense.ExpenseDate <= expenseTo.Value);
        }

        if (expenseCategoryId.HasValue)
        {
            expenses = expenses.Where(expense => expense.CategoryId == expenseCategoryId.Value);
        }

        if (receiptFrom.HasValue)
        {
            receipts = receipts.Where(receipt => receipt.ReceiptDate >= receiptFrom.Value);
        }

        if (receiptTo.HasValue)
        {
            receipts = receipts.Where(receipt => receipt.ReceiptDate <= receiptTo.Value);
        }

        if (receiptCategoryId.HasValue)
        {
            receipts = receipts.Where(receipt => receipt.CategoryId == receiptCategoryId.Value);
        }

        var query =
            from link in links
            join expense in expenses on link.ExpenseId equals expense.Id
            join receipt in receipts on link.ReceiptId equals receipt.Id
            select new LinkDetailRowData(
                link.Id,
                expense.Id,
                expense.Title,
                expense.ExpenseDate,
                receipt.Id,
                receipt.Title,
                receipt.ReceiptDate,
                link.Notes,
                link.AuditTrail.CreatedAt,
                link.AuditTrail.UpdatedAt);

        return query;
    }

    private static ReceiptExpenseLinkDetailResponse MapDetailRow(LinkDetailRowData row)
    {
        return new ReceiptExpenseLinkDetailResponse(
            row.LinkId,
            row.ExpenseId,
            row.ExpenseTitle,
            row.ReceiptId,
            row.ReceiptTitle,
            row.Notes,
            row.CreatedAt,
            row.UpdatedAt);
    }
    private sealed record LinkDetailRowData(
        Guid LinkId,
        Guid ExpenseId,
        string ExpenseTitle,
        DateOnly ExpenseDate,
        Guid ReceiptId,
        string ReceiptTitle,
        DateOnly ReceiptDate,
        string? Notes,
        DateTime CreatedAt,
        DateTime? UpdatedAt);
}
