using System;
using System.Windows;
using ExpenseManager.Desktop.ViewModels.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using WpfApplication = System.Windows.Application;

namespace ExpenseManager.Desktop.Views.Dialogs;

public partial class RecurringExpenseTemplateDialog : Window
{
    public RecurringExpenseTemplateDialog()
        : this(CreateViewModel())
    {
    }

    public RecurringExpenseTemplateDialog(RecurringExpenseTemplateDialogViewModel viewModel)
    {
        var resourceLocator = new Uri("/ExpenseManager.Desktop;component/Views/Dialogs/RecurringExpenseTemplateDialog.xaml", UriKind.Relative);
        WpfApplication.LoadComponent(this, resourceLocator);
        ViewModel = viewModel;
        DataContext = ViewModel;
    }

    public RecurringExpenseTemplateDialogViewModel ViewModel { get; }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.Validate(out var message))
        {
            MessageBox.Show(
                this,
                message,
                ViewModel.DialogTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
    }

    private static RecurringExpenseTemplateDialogViewModel CreateViewModel()
    {
        var services = App.Services ?? throw new InvalidOperationException("Application services are not initialized.");
        return services.GetRequiredService<RecurringExpenseTemplateDialogViewModel>();
    }
}
