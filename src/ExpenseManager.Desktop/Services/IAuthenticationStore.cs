using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ExpenseManager.Desktop.Services;

public interface IAuthenticationStore
{
    Task<IReadOnlyCollection<AuthenticationCredentials>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AuthenticationCredentials?> FindAsync(string userName, CancellationToken cancellationToken = default);

    Task UpsertAsync(AuthenticationCredentials credentials, CancellationToken cancellationToken = default);
}
