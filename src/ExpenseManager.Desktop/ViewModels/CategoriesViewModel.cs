using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExpenseManager.Application.Categories.Models;
using ExpenseManager.Application.Categories.Requests;
using ExpenseManager.Application.Categories.Services;
using ExpenseManager.Desktop.Extensions;
using ExpenseManager.Desktop.Localization;
using ExpenseManager.Desktop.Services;
using ExpenseManager.Desktop.ViewModels.Abstractions;
using ExpenseManager.Desktop.ViewModels.Items;
using ExpenseManager.Desktop.Views.Dialogs;

namespace ExpenseManager.Desktop.ViewModels;

public sealed partial class CategoriesViewModel : ViewModelBase, ILoadableViewModel, ILocalizableViewModel
{
    private readonly ICategoryService _categoryService;
    private readonly IUserInteractionService _interactionService;
    private readonly ILocalizationManager _localization;
    private readonly IUserSessionService _sessionService;
    private readonly TranslationSource _translationSource = TranslationSource.Instance;
    private IReadOnlyCollection<CategoryItem> _lastCategories = Array.Empty<CategoryItem>();

    public ObservableCollection<CategoryItemViewModel> Categories { get; } = new();

    [ObservableProperty]
    private CategoryItemViewModel? _selectedCategory;

    // Inline add form state
    [ObservableProperty]
    private bool _isAddFormVisible;

    [ObservableProperty]
    private string _newCategoryName = string.Empty;

    [ObservableProperty]
    private string _newCategoryDescription = string.Empty;

    [RelayCommand]
    private void ShowAddForm()
    {
        IsAddFormVisible = true;
    }

    [RelayCommand]
    private void CancelAddCategory()
    {
        IsAddFormVisible = false;
        NewCategoryName = string.Empty;
        NewCategoryDescription = string.Empty;
    }

    [RelayCommand]
    private async Task SaveNewCategoryAsync()
    {
        var name = NewCategoryName.Trim();
        var description = NewCategoryDescription.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CATEGORIES"), Translate("ERROR_CATEGORY_NAME_REQUIRED"));
            return;
        }

        try
        {
            await _categoryService.CreateCategoryAsync(new CreateCategoryRequest(name, description ?? string.Empty));
            await ReloadAsync(CancellationToken.None);
            _interactionService.ShowInformation(Translate("NAVIGATION_CATEGORIES"), Translate("INFO_CATEGORY_CREATED"));
            // reset form
            CancelAddCategory();
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CATEGORIES"), exception.Message);
        }
    }

    public CategoriesViewModel(ICategoryService categoryService, IUserInteractionService interactionService, ILocalizationManager localization, IUserSessionService sessionService)
    {
        _categoryService = categoryService;
        _interactionService = interactionService;
        _localization = localization;
        _sessionService = sessionService;

        _translationSource.PropertyChanged += OnTranslationSourceChanged;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await ReloadAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await ReloadAsync(CancellationToken.None);
    }

    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        var dialog = new CategoryEditorDialog();
        var result = dialog.ShowDialog();
        if (result != true)
        {
            return;
        }

        var viewModel = dialog.ViewModel;
        try
        {
            await _categoryService.CreateCategoryAsync(new CreateCategoryRequest(viewModel.Name, viewModel.Description));
            await ReloadAsync(CancellationToken.None);
            _interactionService.ShowInformation(Translate("NAVIGATION_CATEGORIES"), Translate("INFO_CATEGORY_CREATED"));
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CATEGORIES"), exception.Message);
        }
    }

    [RelayCommand]
    private async Task EditCategoryAsync()
    {
        if (SelectedCategory is null)
        {
            return;
        }

        var dialog = new CategoryEditorDialog(SelectedCategory.Name, SelectedCategory.Description);
        var result = dialog.ShowDialog();
        if (result != true)
        {
            return;
        }

        var viewModel = dialog.ViewModel;
        try
        {
            await _categoryService.UpdateCategoryAsync(new UpdateCategoryRequest(SelectedCategory.Id, viewModel.Name, viewModel.Description));
            await ReloadAsync(CancellationToken.None);
            _interactionService.ShowInformation(Translate("NAVIGATION_CATEGORIES"), Translate("INFO_CATEGORY_UPDATED"));
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CATEGORIES"), exception.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync()
    {
        if (SelectedCategory is null)
        {
            return;
        }

        if (SelectedCategory.IsDefault)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CATEGORIES"), Translate("ERROR_CATEGORY_DEFAULT_REMOVE"));
            return;
        }

        try
        {
            await _categoryService.DeleteCategoryAsync(SelectedCategory.Id);
            await ReloadAsync(CancellationToken.None);
            _interactionService.ShowInformation(Translate("NAVIGATION_CATEGORIES"), Translate("INFO_CATEGORY_REMOVED"));
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_CATEGORIES"), exception.Message);
        }
    }

    private async Task ReloadAsync(CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetCategoriesAsync(_sessionService.UserId, cancellationToken);
        _lastCategories = categories;
        ApplyCategories(_lastCategories);
    }

    private string Translate(string key) => _localization.GetString(key);

    private string Translate(string key, params object[] arguments) => _localization.GetString(key, arguments);

    public void RefreshTranslations()
    {
        ApplyCategories(_lastCategories);
    }

    private void ApplyCategories(IEnumerable<CategoryItem> categories)
    {
        Categories.Clear();
        var defaultBadgeText = Translate("CATEGORY_DEFAULT_BADGE");

        foreach (var category in categories)
        {
            var displayName = CategoryLocalization.TranslateName(_localization, category.Name);
            var displayDescription = CategoryLocalization.TranslateDescription(_localization, category.Description);
            var expenseCountText = Translate("CATEGORY_EXPENSE_COUNT_FORMAT", category.ExpenseCount);

            Categories.Add(new CategoryItemViewModel(
                category.Id,
                category.Name,
                category.Description,
                category.IsDefault,
                category.ExpenseCount,
                displayName,
                displayDescription,
                expenseCountText,
                defaultBadgeText));
        }
    }

    private void OnTranslationSourceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null || string.Equals(e.PropertyName, "Item[]", StringComparison.Ordinal))
        {
            RefreshTranslations();
        }
    }
}
