using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExpenseManager.Application.Categories.Services;
using ExpenseManager.Application.Expenses.Models;
using ExpenseManager.Application.Expenses.Requests;
using ExpenseManager.Application.Expenses.Requests.Shared;
using ExpenseManager.Application.Expenses.Services;
using ExpenseManager.Application.Users.Services;
using ExpenseManager.Desktop.Extensions;
using ExpenseManager.Desktop.Localization;
using ExpenseManager.Desktop.Services;
using ExpenseManager.Desktop.ViewModels.Abstractions;
using ExpenseManager.Desktop.ViewModels.Items;
using CategoryOptionViewModel = ExpenseManager.Desktop.ViewModels.Items.CategoryOptionViewModel;
using PaymentMethodOptionViewModel = ExpenseManager.Desktop.ViewModels.Items.PaymentMethodOptionViewModel;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Desktop.ViewModels;

public sealed partial class ExpensesViewModel : ViewModelBase, ILoadableViewModel, ILocalizableViewModel
{
    private readonly IExpenseService _expenseService;
    private readonly IUserSessionService _sessionService;
    private readonly IUserInteractionService _interactionService;
    private readonly ICategoryService _categoryService;
    private readonly IFilePickerService _filePickerService;
    private readonly ILocalizationManager _localization;
    private readonly TranslationSource _translationSource = TranslationSource.Instance;

    private Guid? _userId;
    private IReadOnlyCollection<ExpenseListItem> _lastExpenses = Array.Empty<ExpenseListItem>();

    public ObservableCollection<ExpenseRowViewModel> Expenses { get; } = new();

    [ObservableProperty]
    private ExpenseRowViewModel? _selectedExpense;

    [ObservableProperty]
    private ExpenseDetailsViewModel? _details;

    [ObservableProperty]
    private bool _isFormVisible;

    [ObservableProperty]
    private bool _isBusy;

    public ExpenseFormViewModel Form { get; }

    public ExpensesViewModel(
        IExpenseService expenseService,
        IUserSessionService sessionService,
        IUserInteractionService interactionService,
        ICategoryService categoryService,
        IFilePickerService filePickerService,
        ILocalizationManager localization)
    {
        _expenseService = expenseService;
        _sessionService = sessionService;
        _interactionService = interactionService;
        _categoryService = categoryService;
        _filePickerService = filePickerService;
        _localization = localization;
        _translationSource.PropertyChanged += OnTranslationSourceChanged;

        Form = new ExpenseFormViewModel(_localization);
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _userId = _sessionService.UserId;
        if (_userId is null)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_EXPENSES"), Translate("ERROR_NO_USER_CONFIGURED"));
            return;
        }

        await LoadInternalAsync(_userId.Value, cancellationToken);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_userId is null)
        {
            return;
        }

        await LoadInternalAsync(_userId.Value, CancellationToken.None);
    }

    [RelayCommand]
    private async Task BeginAddExpenseAsync()
    {
        await EnsureUserAsync();
        if (_userId is null)
        {
            return;
        }

        var categories = await LoadCategoriesAsync();
        if (categories.Count == 0)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_EXPENSES"), Translate("ERROR_CREATE_CATEGORY_FIRST"));
            return;
        }

        Form.BeginCreate(categories);
        IsFormVisible = true;
    }

    [RelayCommand(CanExecute = nameof(CanEditSelectedExpense))]
    private async Task BeginEditExpenseAsync()
    {
        if (SelectedExpense is null)
        {
            return;
        }

        await EnsureUserAsync();
        if (_userId is null)
        {
            return;
        }

        var details = await _expenseService.GetExpenseAsync(_userId.Value, SelectedExpense.Id, CancellationToken.None);
        if (details is null)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_EXPENSES"), Translate("ERROR_EXPENSE_LOAD_FAILED"));
            return;
        }

        var categories = await LoadCategoriesAsync();
        if (categories.Count == 0)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_EXPENSES"), Translate("ERROR_CREATE_CATEGORY_FIRST"));
            return;
        }

        Form.BeginEdit(details, categories);
        IsFormVisible = true;
    }

    [RelayCommand(CanExecute = nameof(CanEditSelectedExpense))]
    private async Task DeleteExpenseAsync()
    {
        if (SelectedExpense is null)
        {
            return;
        }

        if (_userId is null)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_EXPENSES"), Translate("ERROR_NO_USER_CONFIGURED"));
            return;
        }

        await _expenseService.DeleteExpenseAsync(_userId.Value, SelectedExpense.Id, CancellationToken.None);
        await RefreshAsync();
    }

    [RelayCommand]
    private void CancelEditing()
    {
        Form.Reset();
        IsFormVisible = false;
    }

    [RelayCommand]
    private async Task SaveExpenseAsync()
    {
        await EnsureUserAsync();
        if (_userId is null)
        {
            return;
        }

        if (!Form.Validate(out var message))
        {
            var displayMessage = message ?? Translate("ERROR_INVALID_FORM");
            _interactionService.ShowInformation(Translate("NAVIGATION_EXPENSES"), displayMessage);
            return;
        }

        try
        {
            IsBusy = true;

            if (Form.IsEditMode)
            {
                var updateRequest = Form.ToUpdateRequest(_userId.Value);
                await _expenseService.UpdateExpenseAsync(updateRequest, CancellationToken.None);
                _interactionService.ShowInformation(Translate("NAVIGATION_EXPENSES"), Translate("INFO_EXPENSE_UPDATED"));
            }
            else
            {
                var createRequest = Form.ToCreateRequest(_userId.Value);
                await _expenseService.CreateExpenseAsync(createRequest, CancellationToken.None);
                _interactionService.ShowInformation(Translate("NAVIGATION_EXPENSES"), Translate("INFO_EXPENSE_CREATED"));
            }

            Form.Reset();
            IsFormVisible = false;

            if (_userId is not null)
            {
                await LoadInternalAsync(_userId.Value, CancellationToken.None);
            }
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_EXPENSES"), exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void RemoveAttachment(ExpenseAttachmentInputViewModel? attachment)
    {
        if (attachment is null)
        {
            return;
        }

        Form.RemoveAttachment(attachment);
    }

    [RelayCommand]
    private void AddAttachment()
    {
        var files = _filePickerService.PickFiles();
        if (files.Count == 0)
        {
            return;
        }

        Form.AddAttachments(files);
    }

    [RelayCommand]
    private void CancelForm()
    {
        Form.Reset();
        IsFormVisible = false;
    }

    [RelayCommand]
    private void OpenAttachment(ExpenseAttachmentItemViewModel? attachment)
    {
        if (attachment is null)
        {
            return;
        }

        if (!System.IO.File.Exists(attachment.FilePath))
        {
            _interactionService.ShowInformation(Translate("LABEL_ATTACHMENTS"), Translate("ERROR_ATTACHMENT_NOT_FOUND"));
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = attachment.FilePath,
            UseShellExecute = true
        });
    }

    private bool CanEditSelectedExpense() => SelectedExpense is not null;

    private async Task EnsureUserAsync()
    {
        if (!_sessionService.IsAuthenticated || _sessionService.UserId is null)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_EXPENSES"), Translate("ERROR_NO_USER_CONFIGURED"));
            return;
        }

        _userId = _sessionService.UserId;
        await Task.CompletedTask;
    }

    private async Task<IReadOnlyCollection<CategoryOptionViewModel>> LoadCategoriesAsync()
    {
    var result = await _categoryService.GetCategoriesAsync(_sessionService.UserId, CancellationToken.None);
        return result
            .Select(category =>
            {
                var displayName = CategoryLocalization.TranslateName(_localization, category.Name);
                return new CategoryOptionViewModel(category.Id, category.Name, displayName);
            })
            .ToList();
    }

    private async Task LoadInternalAsync(Guid userId, CancellationToken cancellationToken)
    {
        var items = await _expenseService.GetExpensesAsync(userId, cancellationToken);
        _lastExpenses = items.ToList();

        ApplyExpenses(_lastExpenses);

        if (Expenses.Count == 0)
        {
            Details = null;
        }
        else if (SelectedExpense is not null)
        {
            await LoadExpenseDetailsAsync(SelectedExpense.Id, cancellationToken);
        }
    }

    private void ApplyExpenses(IEnumerable<ExpenseListItem> items)
    {
        var culture = CultureInfo.CurrentCulture;

        Expenses.Clear();
        foreach (var item in items)
        {
            var amountDisplay = string.Format(culture, "{0:C}", item.Amount);
            var dateDisplay = item.ExpenseDate.ToString("dd/MM/yyyy", culture);
            var dueDateDisplay = item.DueDate?.ToString("dd/MM/yyyy", culture) ?? "-";
            var categoryDisplay = CategoryLocalization.TranslateName(_localization, item.CategoryName);
            var statusDisplay = TranslateExpenseStatus(item.Status);
            var paymentDisplay = TranslatePaymentMethod(item.PaymentMethod);

            Expenses.Add(new ExpenseRowViewModel(
                item.Id,
                item.Title,
                categoryDisplay,
                amountDisplay,
                dateDisplay,
                statusDisplay,
                paymentDisplay,
                dueDateDisplay));
        }
    }

    public void RefreshTranslations()
    {
        ApplyExpenses(_lastExpenses);

        if (SelectedExpense is not null)
        {
            _ = LoadExpenseDetailsAsync(SelectedExpense.Id, CancellationToken.None);
        }

        Form.RefreshPaymentMethodOptions();

        if (IsFormVisible)
        {
            _ = RefreshFormCategoriesAsync();
        }
    }

    private async Task RefreshFormCategoriesAsync()
    {
        var categories = await LoadCategoriesAsync();
        var selectedId = Form.SelectedCategory?.Id;

        Form.Categories.Clear();
        foreach (var category in categories)
        {
            Form.Categories.Add(category);
        }

        if (selectedId.HasValue)
        {
            var id = selectedId.Value;
            Form.SelectedCategory = Form.Categories.FirstOrDefault(category => category.Id == id);
        }
    }

    private async Task LoadExpenseDetailsAsync(Guid expenseId, CancellationToken cancellationToken)
    {
        if (_userId is null)
        {
            return;
        }

        var details = await _expenseService.GetExpenseAsync(_userId.Value, expenseId, cancellationToken);
        if (details is null)
        {
            Details = null;
            return;
        }

        var culture = CultureInfo.CurrentCulture;
        var amountDisplay = string.Format(culture, "{0:C}", details.Amount);
        var expenseDateDisplay = details.ExpenseDate.ToString("dd/MM/yyyy", culture);
        var dueDateDisplay = details.DueDate?.ToString("dd/MM/yyyy", culture);
    var categoryDisplay = CategoryLocalization.TranslateName(_localization, details.CategoryName);
        var statusDisplay = TranslateExpenseStatus(details.Status);
        var paymentDisplay = TranslatePaymentMethod(details.PaymentMethod);

        var viewModel = new ExpenseDetailsViewModel(
            details.Id,
            details.Title,
            details.Description,
            categoryDisplay,
            amountDisplay,
            statusDisplay,
            paymentDisplay,
            expenseDateDisplay,
            dueDateDisplay);

        viewModel.Attachments.Clear();
        foreach (var attachment in details.Attachments.OrderByDescending(item => item.UploadedAt))
        {
            viewModel.Attachments.Add(new ExpenseAttachmentItemViewModel(
                attachment.Id,
                attachment.FileName,
                attachment.FilePath,
                attachment.FileSizeInBytes,
                attachment.UploadedAt));
        }

        Details = viewModel;
    }

    partial void OnSelectedExpenseChanged(ExpenseRowViewModel? value)
    {
        BeginEditExpenseCommand.NotifyCanExecuteChanged();
        DeleteExpenseCommand.NotifyCanExecuteChanged();

        if (value is null)
        {
            Details = null;
            return;
        }

        _ = LoadExpenseDetailsAsync(value.Id, CancellationToken.None);
    }

    public sealed partial class ExpenseFormViewModel : ObservableObject
    {
        private readonly ILocalizationManager _localization;
        private readonly List<Guid> _attachmentsToRemove = new();

        public ExpenseFormViewModel(ILocalizationManager localization)
        {
            _localization = localization;
            PopulatePaymentMethods();
        }

        public ObservableCollection<CategoryOptionViewModel> Categories { get; } = new();
        public ObservableCollection<ExpenseAttachmentInputViewModel> Attachments { get; } = new();
        public ObservableCollection<PaymentMethodOptionViewModel> PaymentMethodOptions { get; } = new();

        public Array CurrencyValues { get; } = Enum.GetValues(typeof(Currency));
        public Array StatusValues { get; } = Enum.GetValues(typeof(ExpenseStatus));

        private RecurrenceType _recurrence = RecurrenceType.None;

        public Guid? ExpenseId { get; private set; }

        [ObservableProperty]
        private CategoryOptionViewModel? _selectedCategory;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string? _description;

        [ObservableProperty]
        private decimal _amount;

        [ObservableProperty]
        private Currency _selectedCurrency = Currency.Eur;

        [ObservableProperty]
        private PaymentMethod _selectedPaymentMethod = PaymentMethod.DebitCard;

        [ObservableProperty]
        private ExpenseStatus _selectedStatus = ExpenseStatus.Approved;

        [ObservableProperty]
        private DateTime _expenseDate = DateTime.Today;

        [ObservableProperty]
        private bool _hasDueDate;

        [ObservableProperty]
        private DateTime _dueDate = DateTime.Today;

        public bool IsEditMode { get; private set; }

        public void BeginCreate(IReadOnlyCollection<CategoryOptionViewModel> categories)
        {
            Reset();
            IsEditMode = false;
            ExpenseId = null;
            PopulateCategories(categories);
            SelectedStatus = ExpenseStatus.Approved;
            _recurrence = RecurrenceType.None;
        }

        public void BeginEdit(ExpenseDetails details, IReadOnlyCollection<CategoryOptionViewModel> categories)
        {
            Reset();
            IsEditMode = true;
            ExpenseId = details.Id;

            PopulateCategories(categories);
            SelectedCategory = Categories.FirstOrDefault(category => category.Id == details.CategoryId);

            Title = details.Title;
            Description = details.Description;
            Amount = details.Amount;
            SelectedCurrency = details.Currency;
            SelectedPaymentMethod = details.PaymentMethod;
            SelectedStatus = details.Status;
            ExpenseDate = details.ExpenseDate.ToDateTime(TimeOnly.MinValue);
            _recurrence = details.Recurrence;

            if (details.DueDate is not null)
            {
                HasDueDate = true;
                DueDate = details.DueDate.Value.ToDateTime(TimeOnly.MinValue);
            }
            else
            {
                HasDueDate = false;
                DueDate = ExpenseDate;
            }

            foreach (var attachment in details.Attachments)
            {
                Attachments.Add(new ExpenseAttachmentInputViewModel(attachment.Id, attachment.FileName, attachment.FilePath, attachment.FileSizeInBytes, false));
            }
        }

        public void Reset()
        {
            ExpenseId = null;
            SelectedCategory = null;
            Title = string.Empty;
            Description = null;
            Amount = 0m;
            SelectedCurrency = Currency.Eur;
            SelectedPaymentMethod = PaymentMethod.DebitCard;
            SelectedStatus = ExpenseStatus.Approved;
            ExpenseDate = DateTime.Today;
            HasDueDate = false;
            DueDate = DateTime.Today;
            _recurrence = RecurrenceType.None;
            Attachments.Clear();
            Categories.Clear();
            _attachmentsToRemove.Clear();
            PopulatePaymentMethods();
        }

        public bool Validate(out string? message)
        {
            message = null;

            if (SelectedCategory is null)
            {
                message = Translate("ERROR_SELECT_CATEGORY");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Title))
            {
                message = Translate("ERROR_EXPENSE_TITLE_REQUIRED");
                return false;
            }

            if (Amount <= 0)
            {
                message = Translate("ERROR_AMOUNT_GREATER_ZERO");
                return false;
            }

            if (HasDueDate && DueDate.Date < ExpenseDate.Date)
            {
                message = Translate("ERROR_DUE_DATE_BEFORE_EXPENSE");
                return false;
            }

            return true;
        }

        public CreateExpenseRequest ToCreateRequest(Guid userId)
        {
            if (SelectedCategory is null)
            {
                throw new InvalidOperationException(Translate("ERROR_CATEGORY_NOT_SELECTED"));
            }

            var expenseDate = DateOnly.FromDateTime(ExpenseDate.Date);
            DateOnly? dueDate = HasDueDate ? DateOnly.FromDateTime(DueDate.Date) : null;

            var attachments = Attachments
                .Select(attachment => new CreateExpenseAttachmentRequest(attachment.FileName, attachment.FilePath, attachment.FileSizeInBytes))
                .ToList();

            return new CreateExpenseRequest(
                userId,
                SelectedCategory.Id,
                Title.Trim(),
                string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                Amount,
                SelectedCurrency,
                SelectedPaymentMethod,
                expenseDate,
                dueDate,
                _recurrence,
                attachments);
        }

        public UpdateExpenseRequest ToUpdateRequest(Guid userId)
        {
            if (ExpenseId is null)
            {
                throw new InvalidOperationException(Translate("ERROR_EXPENSE_NOT_LOADED"));
            }

            if (SelectedCategory is null)
            {
                throw new InvalidOperationException(Translate("ERROR_CATEGORY_NOT_SELECTED"));
            }

            var expenseDate = DateOnly.FromDateTime(ExpenseDate.Date);
            DateOnly? dueDate = HasDueDate ? DateOnly.FromDateTime(DueDate.Date) : null;

            var attachmentsToAdd = Attachments
                .Where(attachment => attachment.IsNew)
                .Select(attachment => new CreateExpenseAttachmentRequest(attachment.FileName, attachment.FilePath, attachment.FileSizeInBytes))
                .ToList();

            return new UpdateExpenseRequest(
                ExpenseId.Value,
                userId,
                SelectedCategory.Id,
                Title.Trim(),
                string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                Amount,
                SelectedCurrency,
                SelectedPaymentMethod,
                expenseDate,
                dueDate,
                _recurrence,
                SelectedStatus,
                attachmentsToAdd,
                _attachmentsToRemove.ToList());
        }

        public void AddAttachments(IReadOnlyCollection<PickedFile> files)
        {
            foreach (var file in files)
            {
                Attachments.Add(new ExpenseAttachmentInputViewModel(null, file.FileName, file.FullPath, file.FileSizeInBytes, true));
            }
        }

        public void RemoveAttachment(ExpenseAttachmentInputViewModel attachment)
        {
            Attachments.Remove(attachment);

            if (!attachment.IsNew && attachment.AttachmentId.HasValue)
            {
                _attachmentsToRemove.Add(attachment.AttachmentId.Value);
            }
        }

        private void PopulateCategories(IReadOnlyCollection<CategoryOptionViewModel> categories)
        {
            Categories.Clear();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }

            SelectedCategory = Categories.Count > 0 ? Categories[0] : null;
        }

        private void PopulatePaymentMethods()
        {
            var previousSelection = SelectedPaymentMethod;

            PaymentMethodOptions.Clear();
            foreach (var method in Enum.GetValues<PaymentMethod>())
            {
                var displayName = TranslatePaymentMethod(method);
                PaymentMethodOptions.Add(new PaymentMethodOptionViewModel(method, displayName));
            }

            if (PaymentMethodOptions.Count == 0)
            {
                return;
            }

            if (PaymentMethodOptions.Any(option => option.Value == previousSelection))
            {
                SelectedPaymentMethod = previousSelection;
            }
            else
            {
                SelectedPaymentMethod = PaymentMethodOptions[0].Value;
            }
        }

        public void RefreshPaymentMethodOptions() => PopulatePaymentMethods();

        private string Translate(string key) => _localization.GetString(key);

        private string TranslatePaymentMethod(PaymentMethod method)
        {
            var key = $"PAYMENT_METHOD_{method.ToString().ToUpperInvariant()}";
            var value = _localization.GetString(key);
            return string.Equals(value, key, StringComparison.Ordinal) ? method.ToString() : value;
        }
    }

    private string Translate(string key) => _localization.GetString(key);

    private string TranslateExpenseStatus(ExpenseStatus status)
    {
        var key = $"EXPENSE_STATUS_{status.ToString().ToUpperInvariant()}";
        return TranslateWithFallback(key, status.ToString());
    }

    private string TranslatePaymentMethod(PaymentMethod method)
    {
        var key = $"PAYMENT_METHOD_{method.ToString().ToUpperInvariant()}";
        return TranslateWithFallback(key, method.ToString());
    }

    private string TranslateWithFallback(string key, string fallback)
    {
        var value = _localization.GetString(key);
        return string.Equals(value, key, StringComparison.Ordinal) ? fallback : value;
    }

    private void OnTranslationSourceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null || string.Equals(e.PropertyName, "Item[]", StringComparison.Ordinal))
        {
            RefreshTranslations();
        }
    }
}
