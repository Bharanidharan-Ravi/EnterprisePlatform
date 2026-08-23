using APIPlatform.Database.Migration.Schema.Models;
using APIPlatform.Database.Migration.Schema.Sql;
using APIPlatform.Database.Migration.Sql.Dialects;
using Xunit;

namespace APIPlatform.Database.Migration.Tests.Schema;

/// <summary>
/// Covers the one place a template table and a caller-defined table differ, and the fact that
/// Template (which columns) and Table (which physical name) are resolved independently.
/// Resolution is pure input -> columns, so every rule here is asserted without a live database.
/// </summary>
public class TableDefinitionResolverTests
{
    private static readonly IMigrationSqlDialect SqlServer = new SqlServerMigrationDialect();

    private static ResolvedTable Resolve(TableDefinition definition)
    {
        Assert.True(TableDefinitionResolver.TryResolve(definition, SqlServer, out var resolved, out var error), error);
        return resolved;
    }

    private static string Error(TableDefinition definition)
    {
        Assert.False(TableDefinitionResolver.TryResolve(definition, SqlServer, out _, out var error));
        return error;
    }

    [Fact]
    public void Template_WithNoTable_UsesTheTemplatesOwnTableName()
    {
        var resolved = Resolve(new TableDefinition { Template = "login" });

        Assert.Equal("Logins", resolved.TableName);
        Assert.Equal("login", resolved.TemplateKey);
        Assert.Contains(resolved.Columns, c => c.Name == "Username");
        Assert.Contains(resolved.Columns, c => c.Name == "PasswordHash");
        Assert.Contains(resolved.Columns, c => c.Name == "PasswordSalt");
    }

    [Fact]
    public void TemplateKey_IsCaseInsensitive()
    {
        Assert.Equal("Logins", Resolve(new TableDefinition { Template = "LOGIN" }).TableName);
    }

    /// <summary>The whole point of separating Template from Table: the same predefined columns
    /// under a name the caller chooses, e.g. a second login table for another tenant.</summary>
    [Fact]
    public void Template_WithTable_UsesTheCallersTableName_NotTheTemplatesOwn()
    {
        var resolved = Resolve(new TableDefinition { Template = "login", Table = "TenantALogins" });

        Assert.Equal("TenantALogins", resolved.TableName);
        Assert.Equal("login", resolved.TemplateKey);
        Assert.Contains(resolved.Columns, c => c.Name == "Username");
    }

    /// <summary>A template's key is a column-set selector, never a table name — supplying it as
    /// Table with no Template must not be silently recognized as that template.</summary>
    [Fact]
    public void TemplateKeyLookingText_UsedAsTableWithNoTemplate_IsTreatedAsAPlainTableName()
    {
        var error = Error(new TableDefinition { Table = "login" });

        Assert.Contains("No template was given and no fields were supplied", error);
    }

    [Fact]
    public void UnknownTemplate_IsRejected_NamingTheKnownTemplates()
    {
        var error = Error(new TableDefinition { Template = "shipping-address" });

        Assert.Contains("Unknown template 'shipping-address'", error);
        Assert.Contains("login", error);
    }

    [Fact]
    public void EveryTable_GetsIdKeyAndAuditColumns()
    {
        var resolved = Resolve(new TableDefinition
        {
            Table = "CustomerFeedback",
            Fields = [new FieldDefinition { Name = "Rating", Type = "int" }]
        });

        var id = Assert.Single(resolved.Columns, c => c.Name == "Id");
        Assert.True(id.PrimaryKey);
        Assert.False(id.Nullable);

        Assert.Contains(resolved.Columns, c => c.Name == "CreatedBy");
        Assert.Contains(resolved.Columns, c => c.Name == "CreatedOnUtc");
        Assert.Contains(resolved.Columns, c => c.Name == "LastModifiedBy");
        Assert.Contains(resolved.Columns, c => c.Name == "LastModifiedOnUtc");
        Assert.Contains(resolved.Columns, c => c.Name == "AdditionalData");
    }

    [Fact]
    public void TemplateTable_AcceptsExtraCallerFields()
    {
        var resolved = Resolve(new TableDefinition
        {
            Template = "login",
            Fields =
            [
                new FieldDefinition { Name = "EmployeeCode", MaxLength = 32 },
                new FieldDefinition { Name = "DepartmentId", Type = "guid" }
            ]
        });

        Assert.Contains(resolved.Columns, c => c.Name == "Username");        // from the template
        Assert.Contains(resolved.Columns, c => c.Name == "EmployeeCode");    // from the request
        Assert.Contains(resolved.Columns, c => c.Name == "DepartmentId");
    }

    [Fact]
    public void ExtraField_CollidingWithTemplateColumn_IsRejected()
    {
        var error = Error(new TableDefinition
        {
            Template = "login",
            Fields = [new FieldDefinition { Name = "username" }]
        });

        Assert.Contains("already part of the 'login' template", error);
    }

    [Fact]
    public void UnknownTable_WithNoFields_IsRejected()
    {
        Assert.Contains("no columns to create", Error(new TableDefinition { Table = "Whatever" }));
    }

    [Fact]
    public void NoTemplate_AndNoTable_IsRejected()
    {
        var error = Error(new TableDefinition { Fields = [new FieldDefinition { Name = "Code" }] });

        Assert.Contains("Table", error);
    }

    [Fact]
    public void CallerPrimaryKey_ReplacesTheGeneratedIdColumn()
    {
        var resolved = Resolve(new TableDefinition
        {
            Table = "LegacyRecord",
            Fields = [new FieldDefinition { Name = "RecordCode", MaxLength = 32, PrimaryKey = true }]
        });

        Assert.DoesNotContain(resolved.Columns, c => c.Name == "Id");
        var key = Assert.Single(resolved.Columns, c => c.PrimaryKey);
        Assert.Equal("RecordCode", key.Name);
    }

    /// <summary>A primary key cannot hold NULL, so the constraint would reject the request's
    /// nullable:true anyway — resolution settles it rather than emitting DDL that fails later.</summary>
    [Fact]
    public void PrimaryKey_IsNeverNullable_EvenWhenRequested()
    {
        var resolved = Resolve(new TableDefinition
        {
            Table = "LegacyRecord",
            Fields = [new FieldDefinition { Name = "RecordCode", PrimaryKey = true, Nullable = true }]
        });

        Assert.False(Assert.Single(resolved.Columns, c => c.PrimaryKey).Nullable);
    }

    [Fact]
    public void MultiplePrimaryKeys_AreRejected()
    {
        var error = Error(new TableDefinition
        {
            Table = "Thing",
            Fields =
            [
                new FieldDefinition { Name = "A", PrimaryKey = true },
                new FieldDefinition { Name = "B", PrimaryKey = true }
            ]
        });

        Assert.Contains("Only one field may be the primary key", error);
    }

    [Fact]
    public void DuplicateFieldNames_AreRejected()
    {
        var error = Error(new TableDefinition
        {
            Table = "Thing",
            Fields = [new FieldDefinition { Name = "Code" }, new FieldDefinition { Name = "code" }]
        });

        Assert.Contains("Duplicate field name", error);
    }

    [Fact]
    public void UnknownFieldType_IsRejected_NamingTheSupportedTypes()
    {
        var error = Error(new TableDefinition
        {
            Table = "Thing",
            Fields = [new FieldDefinition { Name = "Amount", Type = "money" }]
        });

        Assert.Contains("Unknown field type 'money'", error);
        Assert.Contains("decimal", error);
    }

    [Fact]
    public void IncludeAudit_False_OmitsAuditColumns()
    {
        var resolved = Resolve(new TableDefinition
        {
            Table = "Thing",
            Fields = [new FieldDefinition { Name = "Code" }],
            IncludeAudit = false,
            IncludeAdditionalData = false
        });

        Assert.DoesNotContain(resolved.Columns, c => c.Name == "CreatedOnUtc");
        Assert.DoesNotContain(resolved.Columns, c => c.Name == "AdditionalData");
        Assert.Equal(["Id", "Code"], resolved.Columns.Select(c => c.Name));
    }

    /// <summary>
    /// Identifiers cannot be parameterized, so they are concatenated into DDL — this is the
    /// allowlist that makes that safe. Rejection has to happen during resolution, before any SQL
    /// text exists at all.
    /// </summary>
    [Theory]
    [InlineData("Users; DROP TABLE Logins--")]
    [InlineData("Users]--")]
    [InlineData("Users\"--")]
    [InlineData("Users Table")]
    [InlineData("1Users")]
    [InlineData("")]
    public void TableNamesThatAreNotPlainIdentifiers_AreRejected(string tableName)
    {
        var definition = new TableDefinition
        {
            Table = tableName,
            Fields = [new FieldDefinition { Name = "Code" }]
        };

        Assert.False(TableDefinitionResolver.TryResolve(definition, SqlServer, out _, out _));
    }

    [Theory]
    [InlineData("Code; DROP TABLE Logins--")]
    [InlineData("Code]")]
    [InlineData("Code Name")]
    public void FieldNamesThatAreNotPlainIdentifiers_AreRejected(string fieldName)
    {
        var definition = new TableDefinition
        {
            Table = "Thing",
            Fields = [new FieldDefinition { Name = fieldName }]
        };

        Assert.False(TableDefinitionResolver.TryResolve(definition, SqlServer, out _, out _));
    }

    [Fact]
    public void StringLengthOutsideSupportedRange_IsRejected()
    {
        var definition = new TableDefinition
        {
            Table = "Thing",
            Fields = [new FieldDefinition { Name = "Blob", Type = "string", MaxLength = 100_000 }]
        };

        Assert.False(TableDefinitionResolver.TryResolve(definition, SqlServer, out _, out var error));
        Assert.Contains("use type 'text'", error);
    }
}
