using Microsoft.Extensions.DependencyInjection;

namespace ExpenseManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}