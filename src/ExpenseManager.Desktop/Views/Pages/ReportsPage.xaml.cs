using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ExpenseManager.Desktop.ViewModels.Abstractions;

namespace ExpenseManager.Desktop.Views.Pages;

public partial class ReportsPage : UserControl
{
    private bool _hasLoaded;

    public ReportsPage()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_hasLoaded)
        {
            return;
        }

        _hasLoaded = true;

        if (DataContext is ILoadableViewModel loadable)
        {
            await loadable.LoadAsync();
        }
    }
}
