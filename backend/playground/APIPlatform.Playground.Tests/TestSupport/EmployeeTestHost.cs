using APIPlatform.CrudEngine.DependencyInjection;
using APIPlatform.CrudEngine.Defaults;
using APIPlatform.CrudEngine.Engine;
using APIPlatform.CrudEngine.Interfaces;
using APIPlatform.Data.Execution;
using APIPlatform.Data.Options;
using APIPlatform.Foundation.Interfaces;
using APIPlatform.Playground.Defaults;
using APIPlatform.Playground.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace APIPlatform.Playground.Tests.TestSupport;

/// <summary>
/// Builds the same DI graph Playground's AddEmployeeModule() wires at runtime, minus RBAC/ASP.NET
/// Core hosting, with a FakeDatabaseExecutor standing in for SQL Server — this is exactly the
/// distinction phase2.md 33 requires: these tests prove CrudEngine orchestration
/// (metadata resolution -&gt; defaults -&gt; validation -&gt; planning -&gt; execution -&gt; SQL text), not
/// that the SQL actually works against a real database.
/// </summary>
internal sealed class EmployeeTestHost
{
    public FakeDatabaseExecutor Executor { get; } = new();
    public FakeClock Clock { get; } = new();
    public FakeCurrentUser CurrentUser { get; } = new();
    public ServiceProvider Services { get; }

    public EmployeeTestHost()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IClock>(Clock);
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton<ITenantContext>(CurrentUser);
        services.AddSingleton<IDatabaseExecutor>(Executor);
        services.Configure<DatabaseOptions>(o =>
        {
            o.ConnectionString = "unused-in-unit-tests";
            o.Provider = DatabaseProvider.SqlServer;
        });

        services.AddSingleton<IEntityDefinitionProvider, EmployeeEntityDefinitionProvider>();
        services.AddSingleton<IEntityDefaultValueProvider, EmployeeDefaultValueProvider>();

        services.AddCrudEngine();

        Services = services.BuildServiceProvider();
    }

    public ICrudEngine<Models.Employee> CrudEngine => Services.GetRequiredService<ICrudEngine<Models.Employee>>();
}
