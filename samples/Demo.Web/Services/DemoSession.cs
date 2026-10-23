using Basilisque.Core.Auth;
using Basilisque.Demo.Data;

namespace Basilisque.Demo.Web.Services;

[RegisterServiceScoped]
public sealed class DemoSession
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DemoSession(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<T> RunAsync<T>(Guid? userId, Func<DocumentScenarios, Task<T>> action)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        if (userId.HasValue)
            scope.ServiceProvider.GetRequiredService<IWritableUserContext<Guid>>().UserId = userId.Value;
        return await action(scope.ServiceProvider.GetRequiredService<DocumentScenarios>());
    }
}
