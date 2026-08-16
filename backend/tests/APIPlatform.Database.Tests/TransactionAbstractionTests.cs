using System.Reflection;
using APIPlatform.Data.Execution;
using APIPlatform.Data.Transactions;
using Microsoft.Data.SqlClient;
using Sap.Data.Hana;
using Xunit;

namespace APIPlatform.Database.Tests;

/// <summary>
/// Verifies the transaction and executor abstractions stay provider-neutral at the public
/// contract level — no member of IDatabaseTransaction (or the fields backing SqlDatabaseExecutor)
/// exposes a SqlClient- or HANA-specific type, which is what lets the same executor run
/// transactions against either engine.
/// </summary>
public class TransactionAbstractionTests
{
    [Fact]
    public void IDatabaseTransaction_PublicSurface_HasNoProviderSpecificTypes()
    {
        var members = typeof(IDatabaseTransaction).GetMethods();

        Assert.All(members, method =>
        {
            AssertNotProviderSpecific(method.ReturnType, method.Name);
            foreach (var parameter in method.GetParameters())
                AssertNotProviderSpecific(parameter.ParameterType, method.Name);
        });
    }

    [Fact]
    public void IDatabaseTransaction_OnlyExposesCommitRollbackAndDispose()
    {
        // Interface reflection only returns directly-declared members, so DisposeAsync (declared
        // on the inherited IAsyncDisposable) must be pulled in via GetInterfaces() explicitly.
        var methodNames = typeof(IDatabaseTransaction).GetMethods()
            .Concat(typeof(IDatabaseTransaction).GetInterfaces().SelectMany(i => i.GetMethods()))
            .Select(m => m.Name)
            .ToArray();

        Assert.Contains(nameof(IDatabaseTransaction.CommitAsync), methodNames);
        Assert.Contains(nameof(IDatabaseTransaction.RollbackAsync), methodNames);
        Assert.Contains("DisposeAsync", methodNames);
        // No provider-flavored escape hatch (e.g. a "SqlTransaction" or "HanaTransaction" getter).
        Assert.DoesNotContain(methodNames, n => n.Contains("Sql", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methodNames, n => n.Contains("Hana", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IDatabaseExecutor_PublicSurface_HasNoProviderSpecificTypes()
    {
        foreach (var method in typeof(IDatabaseExecutor).GetMethods())
        {
            AssertNotProviderSpecific(method.ReturnType, method.Name);
            foreach (var parameter in method.GetParameters())
                AssertNotProviderSpecific(parameter.ParameterType, method.Name);
        }
    }

    [Fact]
    public void SqlDatabaseExecutor_HasNoSqlServerOrHanaSpecificFields()
    {
        // The common executor (Dapper over IDbConnection) must not hold a SqlConnection,
        // SqlTransaction, HanaConnection, etc. as a typed field — only provider-neutral
        // abstractions (IDatabaseConnectionFactory, DatabaseOptions, ...).
        var fields = typeof(SqlDatabaseExecutor).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotEmpty(fields);
        Assert.All(fields, field => AssertNotProviderSpecific(field.FieldType, field.Name));
    }

    private static void AssertNotProviderSpecific(Type type, string memberName)
    {
        var assemblyName = type.Assembly.GetName().Name ?? string.Empty;
        Assert.False(
            assemblyName.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) ||
            assemblyName.Contains("Sap.Data.Hana", StringComparison.OrdinalIgnoreCase) ||
            type == typeof(SqlConnection) || type == typeof(SqlTransaction) ||
            type == typeof(HanaConnection) || type == typeof(HanaTransaction),
            $"{memberName} exposes provider-specific type '{type}', which breaks provider-agnostic abstraction.");
    }
}
