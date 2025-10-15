using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExpenseManager.Application.Categories.Services;
using ExpenseManager.Application.Categories.Requests;
using ExpenseManager.Application.ReceiptExpenseLinks.Requests;
using ExpenseManager.Application.ReceiptExpenseLinks.Responses;
using ExpenseManager.Application.ReceiptExpenseLinks.Services;
using ExpenseManager.Desktop.Extensions;
using ExpenseManager.Desktop.Localization;
using ExpenseManager.Desktop.Services;
using ExpenseManager.Desktop.ViewModels.Abstractions;
using ExpenseManager.Desktop.ViewModels.Items;
using ExpenseManager.Desktop.Views.Dialogs;

namespace ExpenseManager.Desktop.ViewModels;

public sealed partial class ConnectionsViewModel : ViewModelBase, ILoadableViewModel, ILocalizableViewModel
{
    private readonly IReceiptExpenseLinkService _linkService;
    private readonly IUserSessionService _sessionService;
    private readonly IUserInteractionService _interactionService;
    private readonly ICategoryService _categoryService;
    private readonly ILocalizationManager _localization;
    private readonly TranslationSource _translationSource = TranslationSource.Instance;

    private Guid? _userId;
    private ConnectionsSummaryResponse? _lastSummary;
    private IReadOnlyCollection<LinkSummaryResponse> _lastExpenseSummaries = Array.Empty<LinkSummaryResponse>();
    private IReadOnlyCollection<LinkSummaryResponse> _lastReceiptSummaries = Array.Empty<LinkSummaryResponse>();
    private IReadOnlyCollection<ReceiptExpenseLinkDetailResponse> _lastExpenseLinks = Array.Empty<ReceiptExpenseLinkDetailResponse>();
    private Guid? _lastExpenseLinksExpenseId;
    private IReadOnlyCollection<ReceiptExpenseLinkDetailResponse> _lastReceiptLinks = Array.Empty<ReceiptExpenseLinkDetailResponse>();
    private Guid? _lastReceiptLinksReceiptId;
    private ConnectionDetailItemViewModel? _activeLink;
    private bool _suppressLinkSelection;

    public ConnectionsViewModel(
        IReceiptExpenseLinkService linkService,
        IUserSessionService sessionService,
        IUserInteractionService interactionService,
        ICategoryService categoryService,
        ILocalizationManager localization)
    {
        _linkService = linkService;
        _sessionService = sessionService;
        _interactionService = interactionService;
        _categoryService = categoryService;
        _localization = localization;

        _translationSource.PropertyChanged += OnTranslationSourceChanged;
    }

    public ObservableCollection<SummaryCardItemViewModel> SummaryCards { get; } = new();
    public ObservableCollection<CategoryOptionViewModel> ExpenseCategories { get; } = new();
    public ObservableCollection<CategoryOptionViewModel> ReceiptCategories { get; } = new();

    // Inline add form for expense categories
    [ObservableProperty]
    private bool _isAddExpenseCategoryVisible;

    [ObservableProperty]
    private string _newExpenseCategoryName = string.Empty;

    [ObservableProperty]
    private string _newExpenseCategoryDescription = string.Empty;

    // Inline add form for receipt categories
    [ObservableProperty]
    private bool _isAddReceiptCategoryVisible;

    [ObservableProperty]
    private string _newReceiptCategoryName = string.Empty;

    [ObservableProperty]
    private string _newReceiptCategoryDescription = string.Empty;

    public ObservableCollection<LinkSummaryItemViewModel> Expenses { get; } = new();
    public ObservableCollection<LinkSummaryItemViewModel> Receipts { get; } = new();
    public ObservableCollection<ConnectionDetailItemViewModel> ExpenseLinks { get; } = new();
    public ObservableCollection<ConnectionDetailItemViewModel> ReceiptLinks { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private DateTime? _fromDate;

    [ObservableProperty]
    private DateTime? _toDate;

    [ObservableProperty]
    private bool _onlyOpenExpenses;

    [ObservableProperty]
    private CategoryOptionViewModel? _selectedExpenseCategory;

    [ObservableProperty]
    private CategoryOptionViewModel? _selectedReceiptCategory;

    [ObservableProperty]
    private LinkSummaryItemViewModel? _selectedExpense;

    [ObservableProperty]
    private LinkSummaryItemViewModel? _selectedReceipt;

    [ObservableProperty]
    private ConnectionDetailItemViewModel? _selectedExpenseLink;

    [ObservableProperty]
    private ConnectionDetailItemViewModel? _selectedReceiptLink;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private bool _isEditMode;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            if (!_sessionService.IsAuthenticated || _sessionService.UserId is null)
            {
                ErrorMessage = Translate("ERROR_NO_USER_CONFIGURED");
                return;
            }

            _userId = _sessionService.UserId;

            await LoadCategoriesAsync(cancellationToken).ConfigureAwait(false);
            await ReloadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        if (_userId is null)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CONNECTIONS"), Translate("ERROR_NO_USER_CONFIGURED"));
            return;
        }

        await ReloadAsync(CancellationToken.None).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        FromDate = null;
        ToDate = null;
        OnlyOpenExpenses = false;

        if (ExpenseCategories.Count > 0)
        {
            SelectedExpenseCategory = ExpenseCategories[0];
        }

        if (ReceiptCategories.Count > 0)
        {
            SelectedReceiptCategory = ReceiptCategories[0];
        }

        await ReloadAsync(CancellationToken.None).ConfigureAwait(false);
    }

    [RelayCommand(CanExecute = nameof(CanCreateLink))]
    private async Task CreateLinkAsync()
    {
        if (!EnsureUser())
        {
            return;
        }

        if (SelectedExpense is null || SelectedReceipt is null)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CONNECTIONS"), Translate("ERROR_SELECT_EXPENSE_RECEIPT"));
            return;
        }

        try
        {
            IsBusy = true;

            var request = new CreateReceiptExpenseLinkRequest(
                _userId!.Value,
                SelectedExpense.EntityId,
                SelectedReceipt.EntityId,
                _userId.Value,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes!.Trim());

            await _linkService.CreateAsync(request, CancellationToken.None).ConfigureAwait(false);

            _interactionService.ShowInformation(Translate("NAVIGATION_CONNECTIONS"), Translate("INFO_LINK_CREATED"));

            await ReloadAsync(CancellationToken.None).ConfigureAwait(false);
            ResetForm();
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CONNECTIONS"), exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanUpdateLink))]
    private async Task UpdateLinkAsync()
    {
        if (!EnsureUser())
        {
            return;
        }

        if (_activeLink is null)
        {
            return;
        }

        try
        {
            IsBusy = true;

            var request = new UpdateReceiptExpenseLinkRequest(
                _userId!.Value,
                _activeLink.LinkId,
                _activeLink.ExpenseId,
                _activeLink.ReceiptId,
                _userId.Value,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes!.Trim());

            await _linkService.UpdateAsync(request, CancellationToken.None).ConfigureAwait(false);

            _interactionService.ShowInformation(Translate("NAVIGATION_CONNECTIONS"), Translate("INFO_LINK_UPDATED"));

            await ReloadAsync(CancellationToken.None).ConfigureAwait(false);
            ResetForm();
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CONNECTIONS"), exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteLink))]
    private async Task DeleteLinkAsync()
    {
        if (!EnsureUser() || _activeLink is null)
        {
            return;
        }

        try
        {
            IsBusy = true;

            var request = new DeleteReceiptExpenseLinkRequest(_userId!.Value, _activeLink.LinkId, _userId.Value);
            await _linkService.DeleteAsync(request, CancellationToken.None).ConfigureAwait(false);

            _interactionService.ShowInformation(Translate("NAVIGATION_CONNECTIONS"), Translate("INFO_LINK_REMOVED"));

            await ReloadAsync(CancellationToken.None).ConfigureAwait(false);
            ResetForm();
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CONNECTIONS"), exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ResetLinkForm()
    {
        ResetForm();
    }

    public void RefreshTranslations()
    {
        _ = LoadCategoriesAsync(CancellationToken.None);

        if (_lastSummary is not null)
        {
            ApplySummary(_lastSummary);
        }

        ApplyExpenseSummaries(_lastExpenseSummaries);
        ApplyReceiptSummaries(_lastReceiptSummaries);

        if (_lastExpenseLinksExpenseId.HasValue && _lastExpenseLinksExpenseId == SelectedExpense?.EntityId)
        {
            ApplyExpenseLinks(_lastExpenseLinks);
        }

        if (_lastReceiptLinksReceiptId.HasValue && _lastReceiptLinksReceiptId == SelectedReceipt?.EntityId)
        {
            ApplyReceiptLinks(_lastReceiptLinks);
        }
    }

    private async Task ReloadAsync(CancellationToken cancellationToken)
    {
        if (_userId is null)
        {
            return;
        }

        await LoadSummaryAsync(cancellationToken).ConfigureAwait(false);
        await LoadUnlinkedExpensesAsync(cancellationToken).ConfigureAwait(false);
        await LoadUnlinkedReceiptsAsync(cancellationToken).ConfigureAwait(false);

        var anySelection = SelectedExpense is not null || SelectedReceipt is not null;
        if (SelectedExpense is not null)
        {
            await LoadExpenseLinksAsync(SelectedExpense, cancellationToken).ConfigureAwait(false);
        }

        if (SelectedReceipt is not null)
        {
            await LoadReceiptLinksAsync(SelectedReceipt, cancellationToken).ConfigureAwait(false);
        }

        // If both unlinked lists are empty (no selection), load all links for the current filters so the Linked tab isn't empty
        if (!anySelection)
        {
            await LoadAllLinksAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task LoadSummaryAsync(CancellationToken cancellationToken)
    {
        if (_userId is null)
        {
            return;
        }

        var request = new GetConnectionsSummaryRequest(
            _userId.Value,
            ToDateOnly(FromDate),
            ToDateOnly(ToDate),
            GetCategoryId(SelectedExpenseCategory),
            GetCategoryId(SelectedReceiptCategory));

        var summary = await _linkService.GetSummaryAsync(request, cancellationToken).ConfigureAwait(false);
        _lastSummary = summary;
        ApplySummary(summary);
    }

    private void ApplySummary(ConnectionsSummaryResponse summary)
    {
        var culture = CultureInfo.CurrentUICulture;
        var expenseCoverage = summary.TotalExpenses == 0 ? "0%" : string.Format(culture, "{0:P2}", summary.ExpenseCoveragePercent / 100m);
        var receiptCoverage = summary.TotalReceipts == 0 ? "0%" : string.Format(culture, "{0:P2}", summary.ReceiptCoveragePercent / 100m);
            var totalLinksDisplay = string.Format(culture, "{0}", summary.TotalLinks);
        var unlinkedTotal = summary.UnlinkedExpenses + summary.UnlinkedReceipts;

        SummaryCards.Clear();
        SummaryCards.Add(new SummaryCardItemViewModel(
            Translate("CONNECTIONS_CARD_EXPENSE_COVERAGE"),
            string.Format(culture, "{0}/{1} ({2})", summary.LinkedExpenses, summary.TotalExpenses, expenseCoverage),
            "\uE92C",
            "info"));
        SummaryCards.Add(new SummaryCardItemViewModel(
            Translate("CONNECTIONS_CARD_RECEIPT_COVERAGE"),
            string.Format(culture, "{0}/{1} ({2})", summary.LinkedReceipts, summary.TotalReceipts, receiptCoverage),
            "\uE8C8",
            "info"));
            SummaryCards.Add(new SummaryCardItemViewModel(
                Translate("CONNECTIONS_CARD_TOTAL_LINKS"),
                totalLinksDisplay,
                "\uEB0F",
                summary.TotalLinks > 0 ? "success" : "neutral"));
        SummaryCards.Add(new SummaryCardItemViewModel(
            Translate("CONNECTIONS_CARD_ITEMS_PENDING"),
            string.Format(culture, "{0}", unlinkedTotal),
            "\uE14C",
            unlinkedTotal > 0 ? "danger" : "neutral"));
    }

    private async Task LoadUnlinkedExpensesAsync(CancellationToken cancellationToken)
    {
        if (_userId is null)
        {
            return;
        }

        var request = new GetUnlinkedExpensesRequest(
            _userId.Value,
            ToDateOnly(FromDate),
            ToDateOnly(ToDate),
            GetCategoryId(SelectedExpenseCategory),
            OnlyOpenExpenses);

        var expenses = await _linkService.GetUnlinkedExpensesAsync(request, cancellationToken).ConfigureAwait(false);
        _lastExpenseSummaries = expenses;
        ApplyExpenseSummaries(expenses);
    }

    private void ApplyExpenseSummaries(IEnumerable<LinkSummaryResponse> responses)
    {
        var mapped = responses.Select(MapLinkSummary).ToList();
        var previousSelection = SelectedExpense?.EntityId;

        UpdateCollection(Expenses, mapped);

        if (previousSelection.HasValue)
        {
            SelectedExpense = Expenses.FirstOrDefault(item => item.EntityId == previousSelection.Value);
        }
        else if (Expenses.Count > 0)
        {
            SelectedExpense = Expenses[0];
        }
        else
        {
            SelectedExpense = null;
        }

        CreateLinkCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadUnlinkedReceiptsAsync(CancellationToken cancellationToken)
    {
        if (_userId is null)
        {
            return;
        }

        var request = new GetUnlinkedReceiptsRequest(
            _userId.Value,
            ToDateOnly(FromDate),
            ToDateOnly(ToDate),
            GetCategoryId(SelectedReceiptCategory));

        var receipts = await _linkService.GetUnlinkedReceiptsAsync(request, cancellationToken).ConfigureAwait(false);
        _lastReceiptSummaries = receipts;
        ApplyReceiptSummaries(receipts);
    }

    private void ApplyReceiptSummaries(IEnumerable<LinkSummaryResponse> responses)
    {
        var mapped = responses.Select(MapLinkSummary).ToList();
        var previousSelection = SelectedReceipt?.EntityId;

        UpdateCollection(Receipts, mapped);

        if (previousSelection.HasValue)
        {
            SelectedReceipt = Receipts.FirstOrDefault(item => item.EntityId == previousSelection.Value);
        }
        else if (Receipts.Count > 0)
        {
            SelectedReceipt = Receipts[0];
        }
        else
        {
            SelectedReceipt = null;
        }

        CreateLinkCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadExpenseLinksAsync(LinkSummaryItemViewModel? selected, CancellationToken cancellationToken)
    {
        if (_userId is null || selected is null)
        {
            ExpenseLinks.Clear();
            _lastExpenseLinks = Array.Empty<ReceiptExpenseLinkDetailResponse>();
            _lastExpenseLinksExpenseId = null;
            return;
        }

        try
        {
            var request = new GetLinksByExpenseIdRequest(
                _userId.Value,
                selected.EntityId,
                ToDateOnly(FromDate),
                ToDateOnly(ToDate),
                GetCategoryId(SelectedReceiptCategory));

            var links = await _linkService.GetByExpenseAsync(request, cancellationToken).ConfigureAwait(false);
            _lastExpenseLinks = links;
            _lastExpenseLinksExpenseId = selected.EntityId;
            ApplyExpenseLinks(links);
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CONNECTIONS"), exception.Message);
        }
    }

    private void ApplyExpenseLinks(IEnumerable<ReceiptExpenseLinkDetailResponse> links)
    {
        var mapped = links.Select(MapDetail).ToList();
        var previousLinkId = _activeLink?.LinkId;

        UpdateCollection(ExpenseLinks, mapped);

        if (previousLinkId.HasValue)
        {
            var match = mapped.FirstOrDefault(item => item.LinkId == previousLinkId.Value);
            if (match is not null)
            {
                _suppressLinkSelection = true;
                SelectedExpenseLink = match;
                _suppressLinkSelection = false;
                SetActiveLink(match);
            }
        }
    }

    private async Task LoadReceiptLinksAsync(LinkSummaryItemViewModel? selected, CancellationToken cancellationToken)
    {
        if (_userId is null || selected is null)
        {
            ReceiptLinks.Clear();
            _lastReceiptLinks = Array.Empty<ReceiptExpenseLinkDetailResponse>();
            _lastReceiptLinksReceiptId = null;
            return;
        }

        try
        {
            var request = new GetLinksByReceiptIdRequest(
                _userId.Value,
                selected.EntityId,
                ToDateOnly(FromDate),
                ToDateOnly(ToDate),
                GetCategoryId(SelectedExpenseCategory));

            var links = await _linkService.GetByReceiptAsync(request, cancellationToken).ConfigureAwait(false);
            _lastReceiptLinks = links;
            _lastReceiptLinksReceiptId = selected.EntityId;
            ApplyReceiptLinks(links);
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CONNECTIONS"), exception.Message);
        }
    }

    private void ApplyReceiptLinks(IEnumerable<ReceiptExpenseLinkDetailResponse> links)
    {
        var mapped = links.Select(MapDetail).ToList();
        var previousLinkId = _activeLink?.LinkId;

        UpdateCollection(ReceiptLinks, mapped);

        if (previousLinkId.HasValue)
        {
            var match = mapped.FirstOrDefault(item => item.LinkId == previousLinkId.Value);
            if (match is not null)
            {
                _suppressLinkSelection = true;
                SelectedReceiptLink = match;
                _suppressLinkSelection = false;
                SetActiveLink(match);
            }
        }
    }

    private LinkSummaryItemViewModel MapLinkSummary(LinkSummaryResponse response)
    {
        var culture = CultureInfo.CurrentUICulture;
        var categoryDisplay = response.CategoryName is null
            ? Translate("STATUS_NO_DATA")
            : CategoryLocalization.TranslateName(_localization, response.CategoryName);
        var dateDisplay = response.OccursOn.ToString("d", culture);
        var linkCountDisplay = Translate("CONNECTIONS_LINK_COUNT_FORMAT", response.LinkCount);
        string? extraDisplay = null;

        if (response.CommissionPercent.HasValue)
        {
            extraDisplay = Translate("CONNECTIONS_COMMISSION_DISPLAY", response.CommissionPercent.Value.ToString("0.##", culture));
        }

        return new LinkSummaryItemViewModel(
            response.EntityId,
            response.EntityTitle,
            categoryDisplay,
            dateDisplay,
            linkCountDisplay,
            response.LinkCount,
            response.OccursOn,
            response.CategoryId,
            response.CommissionPercent,
            extraDisplay);
    }

    private ConnectionDetailItemViewModel MapDetail(ReceiptExpenseLinkDetailResponse response)
    {
        return new ConnectionDetailItemViewModel(
            response.LinkId,
            response.ExpenseId,
            response.ExpenseTitle,
            response.ReceiptId,
            response.ReceiptTitle,
            response.Notes,
            response.CreatedAt.ToLocalTime(),
            response.UpdatedAt?.ToLocalTime());
    }

    private async Task LoadCategoriesAsync(CancellationToken cancellationToken)
    {
        if (_userId is null)
        {
            return;
        }

        var items = await _categoryService.GetCategoriesAsync(_userId.Value, cancellationToken).ConfigureAwait(false);
        var translated = items
            .OrderBy(category => category.Name)
            .Select(category => new CategoryOptionViewModel(category.Id, category.Name, CategoryLocalization.TranslateName(_localization, category.Name)))
            .ToList();

    var allExpenseOption = new CategoryOptionViewModel(Guid.Empty, Translate("LABEL_ALL"), Translate("LABEL_ALL"));
    var expenseItems = new List<CategoryOptionViewModel> { allExpenseOption };
        expenseItems.AddRange(translated);

    var allReceiptOption = new CategoryOptionViewModel(Guid.Empty, Translate("LABEL_ALL"), Translate("LABEL_ALL"));
    var receiptItems = new List<CategoryOptionViewModel> { allReceiptOption };
        receiptItems.AddRange(translated);

        UpdateCollection(ExpenseCategories, expenseItems);
        UpdateCollection(ReceiptCategories, receiptItems);

        if (SelectedExpenseCategory is null || ExpenseCategories.All(item => item.Id != SelectedExpenseCategory.Id))
        {
            SelectedExpenseCategory = ExpenseCategories.FirstOrDefault();
        }

        if (SelectedReceiptCategory is null || ReceiptCategories.All(item => item.Id != SelectedReceiptCategory.Id))
        {
            SelectedReceiptCategory = ReceiptCategories.FirstOrDefault();
        }
    }

    private bool EnsureUser()
    {
        if (_userId is not null)
        {
            return true;
        }

        if (!_sessionService.IsAuthenticated || _sessionService.UserId is null)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CONNECTIONS"), Translate("ERROR_NO_USER_CONFIGURED"));
            return false;
        }

        _userId = _sessionService.UserId;
        return true;
    }

    private void ResetForm()
    {
        _suppressLinkSelection = true;
        SelectedExpenseLink = null;
        SelectedReceiptLink = null;
        _suppressLinkSelection = false;

        // Clear active link and form fields
        _activeLink = null;
        Notes = null;
        IsEditMode = false;

        // Also clear the selected summary items so the form shows as empty
        SelectedExpense = null;
        SelectedReceipt = null;

        UpdateLinkCommand.NotifyCanExecuteChanged();
        DeleteLinkCommand.NotifyCanExecuteChanged();
        CreateLinkCommand.NotifyCanExecuteChanged();
    }

    private bool CanCreateLink()
    {
        return !IsBusy && SelectedExpense is not null && SelectedReceipt is not null;
    }

    private bool CanUpdateLink()
    {
        return !IsBusy && _activeLink is not null;
    }

    private bool CanDeleteLink()
    {
        return !IsBusy && _activeLink is not null;
    }

    private void SetActiveLink(ConnectionDetailItemViewModel? link)
    {
        _activeLink = link;

        if (link is null)
        {
            Notes = null;
            IsEditMode = false;
        }
        else
        {
            IsEditMode = true;
            Notes = link.Notes;
        }

        UpdateLinkCommand.NotifyCanExecuteChanged();
        DeleteLinkCommand.NotifyCanExecuteChanged();
    }

    private string Translate(string key)
    {
        return _localization.GetString(key);
    }

    private string Translate(string key, params object[] arguments)
    {
        return _localization.GetString(key, arguments);
    }

    private static void UpdateCollection<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    private static DateOnly? ToDateOnly(DateTime? value)
    {
        return value.HasValue ? DateOnly.FromDateTime(value.Value.Date) : null;
    }

    private static Guid? GetCategoryId(CategoryOptionViewModel? option)
    {
        if (option is null || option.Id == Guid.Empty)
        {
            return null;
        }

        return option.Id;
    }

    private async Task LoadAllLinksAsync(CancellationToken cancellationToken)
    {
        if (_userId is null)
        {
            ExpenseLinks.Clear();
            ReceiptLinks.Clear();
            _lastExpenseLinks = Array.Empty<ReceiptExpenseLinkDetailResponse>();
            _lastReceiptLinks = Array.Empty<ReceiptExpenseLinkDetailResponse>();
            _lastExpenseLinksExpenseId = null;
            _lastReceiptLinksReceiptId = null;
            return;
        }

        try
        {
            var links = await _linkService.GetAllAsync(
                _userId.Value,
                ToDateOnly(FromDate),
                ToDateOnly(ToDate),
                GetCategoryId(SelectedExpenseCategory),
                GetCategoryId(SelectedReceiptCategory),
                cancellationToken).ConfigureAwait(false);

            _lastExpenseLinks = links;
            _lastReceiptLinks = links;
            _lastExpenseLinksExpenseId = null;
            _lastReceiptLinksReceiptId = null;

            ApplyExpenseLinks(links);
            ApplyReceiptLinks(links);
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CONNECTIONS"), exception.Message);
        }
    }

    private void OnTranslationSourceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not null && !string.Equals(e.PropertyName, "Item[]", StringComparison.Ordinal))
        {
            return;
        }

        RefreshTranslations();
    }

    [RelayCommand]
    private async Task AddExpenseCategoryAsync()
    {
        var dialog = new CategoryEditorDialog();
        var result = dialog.ShowDialog();
        if (result != true)
        {
            return;
        }

        try
        {
            var id = await _categoryService.CreateCategoryAsync(new CreateCategoryRequest(dialog.ViewModel.Name, dialog.ViewModel.Description));
            await LoadCategoriesAsync(CancellationToken.None).ConfigureAwait(false);
            SelectedExpenseCategory = ExpenseCategories.FirstOrDefault(c => c.Id == id) ?? SelectedExpenseCategory;
            await ReloadAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CATEGORIES"), exception.Message);
        }
    }

    [RelayCommand]
    private void ShowAddExpenseCategory()
    {
        IsAddExpenseCategoryVisible = true;
        NewExpenseCategoryName = string.Empty;
        NewExpenseCategoryDescription = string.Empty;
    }

    [RelayCommand]
    private void CancelAddExpenseCategory()
    {
        IsAddExpenseCategoryVisible = false;
        NewExpenseCategoryName = string.Empty;
        NewExpenseCategoryDescription = string.Empty;
    }

    [RelayCommand]
    private async Task SaveNewExpenseCategoryAsync()
    {
        var name = NewExpenseCategoryName?.Trim();
        var description = NewExpenseCategoryDescription?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CONNECTIONS"), Translate("ERROR_CATEGORY_NAME_REQUIRED"));
            return;
        }

        try
        {
            var id = await _categoryService.CreateCategoryAsync(new CreateCategoryRequest(name!, description ?? string.Empty));
            await LoadCategoriesAsync(CancellationToken.None).ConfigureAwait(false);
            SelectedExpenseCategory = ExpenseCategories.FirstOrDefault(c => c.Id == id) ?? SelectedExpenseCategory;
            await ReloadAsync(CancellationToken.None).ConfigureAwait(false);
            CancelAddExpenseCategory();
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CONNECTIONS"), exception.Message);
        }
    }

    [RelayCommand]
    private async Task AddReceiptCategoryAsync()
    {
        var dialog = new CategoryEditorDialog();
        var result = dialog.ShowDialog();
        if (result != true)
        {
            return;
        }

        try
        {
            var id = await _categoryService.CreateCategoryAsync(new CreateCategoryRequest(dialog.ViewModel.Name, dialog.ViewModel.Description));
            await LoadCategoriesAsync(CancellationToken.None).ConfigureAwait(false);
            SelectedReceiptCategory = ReceiptCategories.FirstOrDefault(c => c.Id == id) ?? SelectedReceiptCategory;
            await ReloadAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CATEGORIES"), exception.Message);
        }
    }

    [RelayCommand]
    private void ShowAddReceiptCategory()
    {
        IsAddReceiptCategoryVisible = true;
        NewReceiptCategoryName = string.Empty;
        NewReceiptCategoryDescription = string.Empty;
    }

    [RelayCommand]
    private void CancelAddReceiptCategory()
    {
        IsAddReceiptCategoryVisible = false;
        NewReceiptCategoryName = string.Empty;
        NewReceiptCategoryDescription = string.Empty;
    }

    [RelayCommand]
    private async Task SaveNewReceiptCategoryAsync()
    {
        var name = NewReceiptCategoryName?.Trim();
        var description = NewReceiptCategoryDescription?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CONNECTIONS"), Translate("ERROR_CATEGORY_NAME_REQUIRED"));
            return;
        }

        try
        {
            var id = await _categoryService.CreateCategoryAsync(new CreateCategoryRequest(name!, description ?? string.Empty));
            await LoadCategoriesAsync(CancellationToken.None).ConfigureAwait(false);
            SelectedReceiptCategory = ReceiptCategories.FirstOrDefault(c => c.Id == id) ?? SelectedReceiptCategory;
            await ReloadAsync(CancellationToken.None).ConfigureAwait(false);
            CancelAddReceiptCategory();
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CONNECTIONS"), exception.Message);
        }
    }

    partial void OnSelectedExpenseChanged(LinkSummaryItemViewModel? value)
    {
        _ = LoadExpenseLinksAsync(value, CancellationToken.None);
        CreateLinkCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedReceiptChanged(LinkSummaryItemViewModel? value)
    {
        _ = LoadReceiptLinksAsync(value, CancellationToken.None);
        CreateLinkCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedExpenseLinkChanged(ConnectionDetailItemViewModel? value)
    {
        if (_suppressLinkSelection)
        {
            return;
        }

        if (value is not null)
        {
            _suppressLinkSelection = true;
            SelectedReceiptLink = null;
            _suppressLinkSelection = false;
        }

        SetActiveLink(value);
    }

    partial void OnSelectedReceiptLinkChanged(ConnectionDetailItemViewModel? value)
    {
        if (_suppressLinkSelection)
        {
            return;
        }

        if (value is not null)
        {
            _suppressLinkSelection = true;
            SelectedExpenseLink = null;
            _suppressLinkSelection = false;
        }

        SetActiveLink(value);
    }

    partial void OnIsBusyChanged(bool value)
    {
        _ = value;
        CreateLinkCommand.NotifyCanExecuteChanged();
        UpdateLinkCommand.NotifyCanExecuteChanged();
        DeleteLinkCommand.NotifyCanExecuteChanged();
    }
}
