namespace APIPlatform.Database.Migration.Migrations.Notification;

/// <summary>
/// DDL statements for APIPlatform.Notification's three tables, one statement per array entry so
/// <see cref="Services.MigrationRunner"/> can execute them individually. Mirrors — and must be
/// kept in sync with — the human-readable reference schemas at
/// <c>APIPlatform.Notification/Schema/SqlServer/001_CreateNotificationTables.sql</c> and
/// <c>APIPlatform.Notification/Schema/Hana/001_CreateNotificationTables.sql</c>: same table
/// shape, keys, and indexes in both places, only restated here as executable C# instead of a
/// file an operator runs by hand. This package never references APIPlatform.Notification's
/// project or types — it only knows the same three table/column names as opaque strings.
/// </summary>
internal static class NotificationSchemaSql
{
    public static readonly string[] SqlServerStatements =
    [
        """
        CREATE TABLE [Notifications]
        (
            [Id]            NVARCHAR(36)   NOT NULL,
            [Application]   NVARCHAR(64)   NOT NULL,
            [EntityType]    NVARCHAR(128)  NULL,
            [EntityId]      NVARCHAR(128)  NULL,
            [EventType]     NVARCHAR(128)  NOT NULL,
            [Title]         NVARCHAR(400)  NOT NULL,
            [Message]       NVARCHAR(MAX)  NULL,
            [Data]          NVARCHAR(MAX)  NULL,
            [CreatedBy]     NVARCHAR(36)   NULL,
            [CreatedOnUtc]  DATETIME2(3)   NOT NULL,
            CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
        )
        """,
        "CREATE INDEX [IX_Notifications_Application_CreatedOnUtc] ON [Notifications] ([Application], [CreatedOnUtc] DESC)",
        "CREATE INDEX [IX_Notifications_Entity] ON [Notifications] ([Application], [EntityType], [EntityId], [CreatedOnUtc] DESC)",
        """
        CREATE TABLE [NotificationTargets]
        (
            [Id]             NVARCHAR(36)  NOT NULL,
            [NotificationId] NVARCHAR(36)  NOT NULL,
            [TargetKind]     TINYINT       NOT NULL,
            [TargetValue]    NVARCHAR(128) NULL,
            [IsExclusion]    BIT           NOT NULL CONSTRAINT [DF_NotificationTargets_IsExclusion] DEFAULT (0),
            CONSTRAINT [PK_NotificationTargets] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_NotificationTargets_Notifications] FOREIGN KEY ([NotificationId])
                REFERENCES [Notifications] ([Id]) ON DELETE CASCADE
        )
        """,
        "CREATE INDEX [IX_NotificationTargets_Notification] ON [NotificationTargets] ([NotificationId], [IsExclusion], [TargetKind], [TargetValue])",
        """
        CREATE TABLE [NotificationUserStates]
        (
            [UserId]          NVARCHAR(36) NOT NULL,
            [Application]     NVARCHAR(64) NOT NULL,
            [LastReadOnUtc]   DATETIME2(3) NULL,
            [LastSyncedOnUtc] DATETIME2(3) NULL,
            [UpdatedOnUtc]    DATETIME2(3) NOT NULL,
            CONSTRAINT [PK_NotificationUserStates] PRIMARY KEY ([UserId], [Application])
        )
        """
    ];

    public static readonly string[] HanaStatements =
    [
        """
        CREATE COLUMN TABLE "Notifications"
        (
            "Id"            NVARCHAR(36)  NOT NULL,
            "Application"   NVARCHAR(64)  NOT NULL,
            "EntityType"    NVARCHAR(128) NULL,
            "EntityId"      NVARCHAR(128) NULL,
            "EventType"     NVARCHAR(128) NOT NULL,
            "Title"         NVARCHAR(400) NOT NULL,
            "Message"       NCLOB         NULL,
            "Data"          NCLOB         NULL,
            "CreatedBy"     NVARCHAR(36)  NULL,
            "CreatedOnUtc"  TIMESTAMP     NOT NULL,
            CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id")
        )
        """,
        "CREATE INDEX \"IX_Notifications_Application_CreatedOnUtc\" ON \"Notifications\" (\"Application\", \"CreatedOnUtc\" DESC)",
        "CREATE INDEX \"IX_Notifications_Entity\" ON \"Notifications\" (\"Application\", \"EntityType\", \"EntityId\", \"CreatedOnUtc\" DESC)",
        """
        CREATE COLUMN TABLE "NotificationTargets"
        (
            "Id"             NVARCHAR(36)  NOT NULL,
            "NotificationId" NVARCHAR(36)  NOT NULL,
            "TargetKind"     TINYINT       NOT NULL,
            "TargetValue"    NVARCHAR(128) NULL,
            "IsExclusion"    BOOLEAN       NOT NULL DEFAULT FALSE,
            CONSTRAINT "PK_NotificationTargets" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_NotificationTargets_Notifications" FOREIGN KEY ("NotificationId")
                REFERENCES "Notifications" ("Id") ON DELETE CASCADE
        )
        """,
        "CREATE INDEX \"IX_NotificationTargets_Notification\" ON \"NotificationTargets\" (\"NotificationId\", \"IsExclusion\", \"TargetKind\", \"TargetValue\")",
        """
        CREATE COLUMN TABLE "NotificationUserStates"
        (
            "UserId"          NVARCHAR(36) NOT NULL,
            "Application"     NVARCHAR(64) NOT NULL,
            "LastReadOnUtc"   TIMESTAMP    NULL,
            "LastSyncedOnUtc" TIMESTAMP    NULL,
            "UpdatedOnUtc"    TIMESTAMP    NOT NULL,
            CONSTRAINT "PK_NotificationUserStates" PRIMARY KEY ("UserId", "Application")
        )
        """
    ];
}
