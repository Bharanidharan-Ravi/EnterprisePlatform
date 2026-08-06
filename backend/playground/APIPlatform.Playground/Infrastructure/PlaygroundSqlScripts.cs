namespace APIPlatform.Playground.Infrastructure;

public static class PlaygroundSqlScripts
{
    public const string CreateTableScript = $@"
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{PlaygroundConstants.ValidationTableName}' AND xtype='U')
        BEGIN
            CREATE TABLE [{PlaygroundConstants.ValidationTableName}] (
                [Id] UNIQUEIDENTIFIER PRIMARY KEY,
                [Name] NVARCHAR(255) NOT NULL,
                [Value] NVARCHAR(MAX) NULL,
                [CreatedOn] DATETIMEOFFSET NOT NULL
            );
        END
    ";

    public const string InsertScript = $@"
        INSERT INTO [{PlaygroundConstants.ValidationTableName}] (Id, Name, Value, CreatedOn)
        VALUES (@Id, @Name, @Value, @CreatedOn)
    ";

    public const string UpdateScript = $@"
        UPDATE [{PlaygroundConstants.ValidationTableName}]
        SET Name = @Name,
            Value = @Value,
            CreatedOn = @CreatedOn
        WHERE Id = @Id
    ";

    public const string DeleteScript = $@"
        DELETE FROM [{PlaygroundConstants.ValidationTableName}]
        WHERE Id = @Id
    ";

    public const string GetByIdScript = $@"
        SELECT Id, Name, Value, CreatedOn
        FROM [{PlaygroundConstants.ValidationTableName}]
        WHERE Id = @Id
    ";

    public const string GetAllScript = $@"
        SELECT Id, Name, Value, CreatedOn
        FROM [{PlaygroundConstants.ValidationTableName}]
    ";

    /// <summary>
    /// Automatically generated summary.
    /// </summary>
    public const string CountScript = $@"
        SELECT COUNT(*)
        FROM [{PlaygroundConstants.ValidationTableName}]
    ";
}
