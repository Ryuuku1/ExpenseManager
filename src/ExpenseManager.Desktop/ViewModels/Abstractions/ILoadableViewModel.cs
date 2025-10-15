using System.Threading;
using System.Threading.Tasks;

namespace ExpenseManager.Desktop.ViewModels.Abstractions;

public interface ILoadableViewModel
{
    Task LoadAsync(CancellationToken cancellationToken = default);
}
