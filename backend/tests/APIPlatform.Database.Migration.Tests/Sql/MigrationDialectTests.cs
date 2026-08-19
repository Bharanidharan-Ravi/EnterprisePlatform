using APIPlatform.Data.Options;
using APIPlatform.Database.Migration.Sql.Dialects;
using Microsoft.Extensions.Options;
using Xunit;

namespace APIPlatform.Database.Migration.Tests.Sql;

public class MigrationDialectTests
{
    [Fact]
    public void SqlServerMigrationDialect_QuotesWithBrackets_AndSupportsTransactionalDdl()
    {
        var dialect = new SqlServerMigrationDialect();

        Assert.Equal("[Foo]", dialect.QuoteIdentifier("Foo"));
        Assert.True(dialect.SupportsTransactionalDdl);
    }

    [Fact]
    public void HanaMigrationDialect_QuotesWithDoubleQuotes_AndDoesNotSupportTransactionalDdl()
    {
        var dialect = new HanaMigrationDialect();

        Assert.Equal("\"Foo\"", dialect.QuoteIdentifier("Foo"));
        Assert.False(dialect.SupportsTransactionalDdl);
    }

    [Fact]
    public void Resolver_SqlServerProvider_ResolvesSqlServerDialect()
    {
        var resolver = new MigrationSqlDialectResolver(Options.Create(new DatabaseOptions
        {
            ConnectionString = "unused",
            Provider = DatabaseProvider.SqlServer
        }));

        Assert.IsType<SqlServerMigrationDialect>(resolver.Resolve());
    }

    [Fact]
    public void Resolver_HanaProvider_ResolvesHanaDialect()
    {
        var resolver = new MigrationSqlDialectResolver(Options.Create(new DatabaseOptions
        {
            ConnectionString = "unused",
            Provider = DatabaseProvider.Hana
        }));

        Assert.IsType<HanaMigrationDialect>(resolver.Resolve());
    }
}
