using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExpenseManager.Application.Categories.Services;
using ExpenseManager.Application.Receipts.Models;
using ExpenseManager.Application.Receipts.Requests;
using ExpenseManager.Application.Receipts.Services;
using ExpenseManager.Desktop.Extensions;
using ExpenseManager.Desktop.Localization;
using ExpenseManager.Desktop.Services;
using ExpenseManager.Desktop.ViewModels.Abstractions;
using ExpenseManager.Desktop.ViewModels.Items;
using ExpenseManager.Domain.Enumerations;

namespace ExpenseManager.Desktop.ViewModels;

public sealed partial class ReceiptsViewModel : ViewModelBase, ILoadableViewModel, ILocalizableViewModel
{
    private readonly IReceiptService _receiptService;
    private readonly ICategoryService _categoryService;
    private readonly IUserSessionService _sessionService;
    private readonly IUserInteractionService _interactionService;
    private readonly IFilePickerService _filePickerService;
    private readonly ILocalizationManager _localization;
    private readonly TranslationSource _translationSource = TranslationSource.Instance;

    private IReadOnlyCollection<ReceiptListItem> _lastReceipts = Array.Empty<ReceiptListItem>();
    private IReadOnlyCollection<CategoryOptionViewModel> _lastCategories = Array.Empty<CategoryOptionViewModel>();
    private Guid? _editingReceiptId;

    public ReceiptsViewModel(
        IReceiptService receiptService,
        ICategoryService categoryService,
        IUserSessionService sessionService,
        IUserInteractionService interactionService,
        IFilePickerService filePickerService,
        ILocalizationManager localization)
    {
        _receiptService = receiptService;
        _categoryService = categoryService;
        _sessionService = sessionService;
        _interactionService = interactionService;
        _filePickerService = filePickerService;
        _localization = localization;

        _translationSource.PropertyChanged += OnTranslationSourceChanged;
        SelectedCurrency = Currency.Eur;
        ReceiptDate = DateTime.Today;
        UpdateAttachmentSummary();
    }

    public ObservableCollection<ReceiptListItemViewModel> Receipts { get; } = new();
    public ObservableCollection<CategoryOptionViewModel> Categories { get; } = new();

    public Array CurrencyValues { get; } = Enum.GetValues(typeof(Currency));

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private ReceiptListItemViewModel? _selectedReceipt;

    [ObservableProperty]
    private bool _isFormVisible;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _titleInput = string.Empty;

    [ObservableProperty]
    private string _descriptionInput = string.Empty;

    [ObservableProperty]
    private string _referenceInput = string.Empty;

    [ObservableProperty]
    private string _vendorInput = string.Empty;

    [ObservableProperty]
    private DateTime _receiptDate;

    [ObservableProperty]
    private string _amountInput = string.Empty;

    [ObservableProperty]
    private string _commissionPercentInput = string.Empty;

    [ObservableProperty]
    private Currency _selectedCurrency;

    [ObservableProperty]
    private CategoryOptionViewModel? _selectedCategory;

    [ObservableProperty]
    private string? _attachmentFileName;

    [ObservableProperty]
    private string? _attachmentFilePath;

    [ObservableProperty]
    private long? _attachmentFileSizeInBytes;

    [ObservableProperty]
    private string _attachmentSummary = string.Empty;

    public bool HasAttachment => !string.IsNullOrWhiteSpace(AttachmentFilePath);

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
                Receipts.Clear();
                Categories.Clear();
                return;
            }

            await LoadCategoriesAsync(cancellationToken).ConfigureAwait(false);
            await LoadReceiptsAsync(cancellationToken).ConfigureAwait(false);
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
    private async Task RefreshAsync()
    {
        if (_sessionService.UserId is null)
        {
            return;
        }

        await LoadReceiptsAsync(CancellationToken.None).ConfigureAwait(false);
    }

    [RelayCommand]
    private void AttachDocument()
    {
        if (!EnsureUser())
        {
            return;
        }

        var files = _filePickerService.PickFiles();
        var file = files.FirstOrDefault();
        if (file is null)
        {
            return;
        }

        AttachmentFileName = file.FileName;
        AttachmentFilePath = file.FullPath;
        AttachmentFileSizeInBytes = file.FileSizeInBytes;
    }

    [RelayCommand]
    private void ClearAttachment()
    {
        AttachmentFileName = null;
        AttachmentFilePath = null;
        AttachmentFileSizeInBytes = null;
    }

    [RelayCommand]
    private void OpenAttachment()
    {
        if (string.IsNullOrWhiteSpace(AttachmentFilePath))
        {
            return;
        }

        if (!File.Exists(AttachmentFilePath))
        {
            _interactionService.ShowInformation(Translate("LABEL_RECEIPT_DOCUMENT"), Translate("ERROR_ATTACHMENT_NOT_FOUND"));
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = AttachmentFilePath,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("LABEL_RECEIPT_DOCUMENT"), exception.Message);
        }
    }

    [RelayCommand]
    private void NewReceipt()
    {
        if (!EnsureUser())
        {
            return;
        }

        ResetForm();
        IsEditing = false;
        _editingReceiptId = null;
        IsFormVisible = true;
    }

    [RelayCommand]
    private async Task EditReceiptAsync()
    {
        if (!EnsureUser())
        {
            return;
        }

        if (SelectedReceipt is null)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_RECEIPTS"), Translate("ERROR_RECEIPT_NOT_SELECTED"));
            return;
        }

        try
        {
            IsBusy = true;
            var userId = _sessionService.UserId!.Value;
            var details = await _receiptService.GetReceiptAsync(userId, SelectedReceipt.Id, CancellationToken.None).ConfigureAwait(false);
            if (details is null)
            {
                _interactionService.ShowInformation(Translate("NAVIGATION_RECEIPTS"), Translate("ERROR_RECEIPT_NOT_SELECTED"));
                return;
            }

            _editingReceiptId = details.Id;
            TitleInput = details.Title;
            DescriptionInput = details.Description ?? string.Empty;
            ReferenceInput = details.ReferenceNumber ?? string.Empty;
            VendorInput = details.Vendor ?? string.Empty;
            ReceiptDate = details.ReceiptDate.ToDateTime(TimeOnly.MinValue);
            AmountInput = details.NetAmount.ToString("0.##", CultureInfo.CurrentCulture);
            CommissionPercentInput = details.CommissionPercent?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;
            SelectedCurrency = details.Currency;
            SelectedCategory = Categories.FirstOrDefault(option => option.Id == details.CategoryId);
            AttachmentFileName = details.FileName;
            AttachmentFilePath = details.FilePath;
            AttachmentFileSizeInBytes = details.FileSizeInBytes;
            IsEditing = true;
            IsFormVisible = true;
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_RECEIPTS"), exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveReceiptAsync()
    {
        if (!EnsureUser())
        {
            return;
        }

        if (!TryBuildRequest(out var createRequest, out var updateRequest))
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_RECEIPTS"), Translate("ERROR_RECEIPT_FORM_INVALID"));
            return;
        }

        try
        {
            IsBusy = true;
            if (IsEditing && updateRequest is not null)
            {
                await _receiptService.UpdateReceiptAsync(updateRequest, CancellationToken.None).ConfigureAwait(false);
                _interactionService.ShowInformation(Translate("NAVIGATION_RECEIPTS"), Translate("INFO_RECEIPT_UPDATED"));
            }
            else if (createRequest is not null)
            {
                await _receiptService.CreateReceiptAsync(createRequest, CancellationToken.None).ConfigureAwait(false);
                _interactionService.ShowInformation(Translate("NAVIGATION_RECEIPTS"), Translate("INFO_RECEIPT_CREATED"));
            }

            await LoadReceiptsAsync(CancellationToken.None).ConfigureAwait(false);
            ResetForm();
            IsFormVisible = false;
            IsEditing = false;
            _editingReceiptId = null;
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_RECEIPTS"), exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        ResetForm();
        IsFormVisible = false;
        IsEditing = false;
        _editingReceiptId = null;
    }

    [RelayCommand]
    private async Task DeleteReceiptAsync()
    {
        if (!EnsureUser())
        {
            return;
        }

        if (SelectedReceipt is null)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_RECEIPTS"), Translate("ERROR_RECEIPT_NOT_SELECTED"));
            return;
        }

        var confirmed = _interactionService.Confirm(Translate("NAVIGATION_RECEIPTS"), Translate("CONFIRM_RECEIPT_DELETE"));
        if (!confirmed)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await _receiptService.DeleteReceiptAsync(_sessionService.UserId!.Value, SelectedReceipt.Id, CancellationToken.None).ConfigureAwait(false);
            _interactionService.ShowInformation(Translate("NAVIGATION_RECEIPTS"), Translate("INFO_RECEIPT_DELETED"));
            await LoadReceiptsAsync(CancellationToken.None).ConfigureAwait(false);
            if (_editingReceiptId == SelectedReceipt.Id)
            {
                CancelEdit();
            }
        }
        catch (Exception exception)
        {
            _interactionService.ShowInformation(Translate("NAVIGATION_RECEIPTS"), exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void RefreshTranslations()
    {
        ApplyCategories(_lastCategories);
        ApplyReceipts(_lastReceipts);
        UpdateAttachmentSummary();
    }

    private async Task LoadCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetCategoriesAsync(_sessionService.UserId, cancellationToken).ConfigureAwait(false);
        var translated = new List<CategoryOptionViewModel>
        {
            new(Guid.Empty, Translate("LABEL_NONE"), Translate("LABEL_NONE"))
        };

        translated.AddRange(categories.Select(category => new CategoryOptionViewModel(
            category.Id,
            category.Name,
            CategoryLocalization.TranslateName(_localization, category.Name))));

        _lastCategories = translated;
        ApplyCategories(translated);
    }

    private async Task LoadReceiptsAsync(CancellationToken cancellationToken)
    {
        var receipts = await _receiptService.GetReceiptsAsync(_sessionService.UserId!.Value, cancellationToken).ConfigureAwait(false);
        _lastReceipts = receipts;
        ApplyReceipts(receipts);
    }

    private void ApplyCategories(IEnumerable<CategoryOptionViewModel> categories)
    {
        var previouslySelected = SelectedCategory?.Id;
        Categories.Clear();
        foreach (var category in categories)
        {
            Categories.Add(category);
        }

        SelectedCategory = Categories.FirstOrDefault(option => option.Id == previouslySelected) ?? Categories.FirstOrDefault();
    }

    private void ApplyReceipts(IEnumerable<ReceiptListItem> receipts)
    {
        Receipts.Clear();
        var culture = CultureInfo.CurrentUICulture;
        foreach (var receipt in receipts)
        {
            var categoryDisplay = string.IsNullOrWhiteSpace(receipt.CategoryName)
                ? Translate("STATUS_NO_DATA")
                : CategoryLocalization.TranslateName(_localization, receipt.CategoryName);
            var dateDisplay = receipt.ReceiptDate.ToString("d", culture);
            var amountDisplay = FormatCurrency(receipt.NetAmount, receipt.Currency);
            var commissionDisplay = receipt.CommissionPercent.HasValue
                ? string.Format(CultureInfo.CurrentCulture, Translate("RECEIPT_COMMISSION_FORMAT"), receipt.CommissionPercent.Value.ToString("0.##", CultureInfo.CurrentCulture))
                : null;

            Receipts.Add(new ReceiptListItemViewModel(
                receipt.Id,
                receipt.Title,
                categoryDisplay,
                amountDisplay,
                dateDisplay,
                receipt.Vendor,
                receipt.ReferenceNumber,
                commissionDisplay));
        }
    }

    partial void OnAttachmentFileNameChanged(string? value)
    {
        _ = value;
        UpdateAttachmentSummary();
    }

    partial void OnAttachmentFilePathChanged(string? value)
    {
        _ = value;
        UpdateAttachmentSummary();
    }

    partial void OnAttachmentFileSizeInBytesChanged(long? value)
    {
        _ = value;
        UpdateAttachmentSummary();
    }

    private void UpdateAttachmentSummary()
    {
        AttachmentSummary = BuildAttachmentSummary();
        OnPropertyChanged(nameof(HasAttachment));
    }

    private string BuildAttachmentSummary()
    {
        if (string.IsNullOrWhiteSpace(AttachmentFileName) || string.IsNullOrWhiteSpace(AttachmentFilePath))
        {
            return Translate("STATUS_NO_DOCUMENT_ATTACHED");
        }

        if (AttachmentFileSizeInBytes is { } size && size > 0)
        {
            return $"{AttachmentFileName} · {FormatFileSize(size)}";
        }

        return AttachmentFileName;
    }

    private static string FormatFileSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }

    private bool TryBuildRequest(out CreateReceiptRequest? createRequest, out UpdateReceiptRequest? updateRequest)
    {
        createRequest = null;
        updateRequest = null;

        if (string.IsNullOrWhiteSpace(TitleInput))
        {
            return false;
        }

        if (!TryParseDecimal(AmountInput, out var amount) || amount < 0)
        {
            return false;
        }

        decimal? commissionPercent = null;
        if (!string.IsNullOrWhiteSpace(CommissionPercentInput))
        {
            if (!TryParseDecimal(CommissionPercentInput, out var parsedCommission))
            {
                return false;
            }

            if (parsedCommission < 0 || parsedCommission > 100)
            {
                return false;
            }

            commissionPercent = parsedCommission;
        }

    Guid? categoryId = SelectedCategory is null || SelectedCategory.Id == Guid.Empty ? null : SelectedCategory.Id;
        var receiptDate = DateOnly.FromDateTime(ReceiptDate);
        var fileName = string.IsNullOrWhiteSpace(AttachmentFileName) ? null : AttachmentFileName.Trim();
        var filePath = string.IsNullOrWhiteSpace(AttachmentFilePath) ? null : AttachmentFilePath.Trim();
    long? fileSize = AttachmentFileSizeInBytes is { } sizeValue && sizeValue > 0 ? sizeValue : null;

        if (IsEditing && _editingReceiptId.HasValue)
        {
            updateRequest = new UpdateReceiptRequest(
                _sessionService.UserId!.Value,
                _editingReceiptId.Value,
                categoryId,
                TitleInput.Trim(),
                string.IsNullOrWhiteSpace(DescriptionInput) ? null : DescriptionInput.Trim(),
                string.IsNullOrWhiteSpace(ReferenceInput) ? null : ReferenceInput.Trim(),
                string.IsNullOrWhiteSpace(VendorInput) ? null : VendorInput.Trim(),
                receiptDate,
                amount,
                SelectedCurrency,
                commissionPercent,
                fileName,
                filePath,
                fileSize);
            return true;
        }

        createRequest = new CreateReceiptRequest(
            _sessionService.UserId!.Value,
            categoryId,
            TitleInput.Trim(),
            string.IsNullOrWhiteSpace(DescriptionInput) ? null : DescriptionInput.Trim(),
            string.IsNullOrWhiteSpace(ReferenceInput) ? null : ReferenceInput.Trim(),
            string.IsNullOrWhiteSpace(VendorInput) ? null : VendorInput.Trim(),
            receiptDate,
            amount,
            SelectedCurrency,
            commissionPercent,
            fileName,
            filePath,
            fileSize);
        return true;
    }

    private void ResetForm()
    {
        TitleInput = string.Empty;
        DescriptionInput = string.Empty;
        ReferenceInput = string.Empty;
        VendorInput = string.Empty;
        AmountInput = string.Empty;
    CommissionPercentInput = string.Empty;
        SelectedCurrency = Currency.Eur;
        ReceiptDate = DateTime.Today;
        SelectedCategory = Categories.FirstOrDefault();
        AttachmentFileName = null;
        AttachmentFilePath = null;
        AttachmentFileSizeInBytes = null;
    }

    private bool EnsureUser()
    {
        if (_sessionService.IsAuthenticated && _sessionService.UserId is not null)
        {
            return true;
        }

        _interactionService.ShowInformation(Translate("NAVIGATION_RECEIPTS"), Translate("ERROR_NO_USER_CONFIGURED"));
        return false;
    }

    private string Translate(string key) => _localization.GetString(key);

    private void OnTranslationSourceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null || string.Equals(e.PropertyName, "Item[]", StringComparison.Ordinal))
        {
            RefreshTranslations();
        }
    }

    private string FormatCurrency(decimal amount, Currency currency)
    {
        var culture = CultureInfo.CurrentUICulture;
        var format = (NumberFormatInfo)culture.NumberFormat.Clone();
        format.CurrencySymbol = currency switch
        {
            Currency.Eur => "€",
            Currency.Gbp => "£",
            Currency.Brl => "R$",
            _ => "$"
        };

        return amount.ToString("C", format);
    }

    private static bool TryParseDecimal(string input, out decimal result)
    {
        result = 0m;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // Try to parse with invariant culture first (dot separator)
        if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
        {
            return true;
        }

        // If that fails, try with current culture (comma separator)
        if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out result))
        {
            return true;
        }

        // As a last resort, replace commas with dots and try invariant culture
        string normalized = input.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }
}
