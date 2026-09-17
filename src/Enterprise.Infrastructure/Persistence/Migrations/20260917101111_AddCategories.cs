using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enterprise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Older local DBs may already have Categories from a removed migration
            // (20260916173223_AddCategories) that included ServiceId/ParentCategoryId.
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[Categories]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Categories] (
                        [Id] uniqueidentifier NOT NULL,
                        [ProviderId] uniqueidentifier NOT NULL,
                        [DisplayOrder] int NOT NULL CONSTRAINT [DF_Categories_DisplayOrder] DEFAULT 0,
                        [IsActive] bit NOT NULL CONSTRAINT [DF_Categories_IsActive] DEFAULT CAST(1 AS bit),
                        [IsDeleted] bit NOT NULL,
                        [DeletedAtUtc] datetime2 NULL,
                        [DeletedBy] nvarchar(max) NULL,
                        [CreatedAtUtc] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(max) NULL,
                        [LastModifiedAtUtc] datetime2 NULL,
                        [LastModifiedBy] nvarchar(max) NULL,
                        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Categories_Providers_ProviderId]
                            FOREIGN KEY ([ProviderId]) REFERENCES [Providers] ([Id]) ON DELETE NO ACTION
                    );

                    CREATE INDEX [IX_Categories_DisplayOrder] ON [Categories] ([DisplayOrder]);
                    CREATE INDEX [IX_Categories_IsActive] ON [Categories] ([IsActive]);
                    CREATE INDEX [IX_Categories_ProviderId] ON [Categories] ([ProviderId]);
                END
                ELSE
                BEGIN
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Categories_MarketplaceServices_ServiceId')
                        ALTER TABLE [Categories] DROP CONSTRAINT [FK_Categories_MarketplaceServices_ServiceId];

                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Categories_Categories_ParentCategoryId')
                        ALTER TABLE [Categories] DROP CONSTRAINT [FK_Categories_Categories_ParentCategoryId];

                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Categories_ServiceId' AND object_id = OBJECT_ID(N'[dbo].[Categories]'))
                        DROP INDEX [IX_Categories_ServiceId] ON [Categories];

                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Categories_ParentCategoryId' AND object_id = OBJECT_ID(N'[dbo].[Categories]'))
                        DROP INDEX [IX_Categories_ParentCategoryId] ON [Categories];

                    IF COL_LENGTH(N'Categories', N'ServiceId') IS NOT NULL
                        ALTER TABLE [Categories] DROP COLUMN [ServiceId];

                    IF COL_LENGTH(N'Categories', N'ParentCategoryId') IS NOT NULL
                        ALTER TABLE [Categories] DROP COLUMN [ParentCategoryId];

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Categories_DisplayOrder' AND object_id = OBJECT_ID(N'[dbo].[Categories]'))
                        CREATE INDEX [IX_Categories_DisplayOrder] ON [Categories] ([DisplayOrder]);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Categories_IsActive' AND object_id = OBJECT_ID(N'[dbo].[Categories]'))
                        CREATE INDEX [IX_Categories_IsActive] ON [Categories] ([IsActive]);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Categories_ProviderId' AND object_id = OBJECT_ID(N'[dbo].[Categories]'))
                        CREATE INDEX [IX_Categories_ProviderId] ON [Categories] ([ProviderId]);
                END

                IF OBJECT_ID(N'[dbo].[CategoryTranslations]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [CategoryTranslations] (
                        [CategoryId] uniqueidentifier NOT NULL,
                        [LanguageCode] nvarchar(5) NOT NULL,
                        [Name] nvarchar(200) NOT NULL,
                        [Description] nvarchar(2000) NULL,
                        CONSTRAINT [PK_CategoryTranslations] PRIMARY KEY ([CategoryId], [LanguageCode]),
                        CONSTRAINT [FK_CategoryTranslations_Categories_CategoryId]
                            FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_CategoryTranslations_LanguageCode_Name]
                        ON [CategoryTranslations] ([LanguageCode], [Name]);
                END
                ELSE IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_CategoryTranslations_LanguageCode_Name'
                      AND object_id = OBJECT_ID(N'[dbo].[CategoryTranslations]'))
                BEGIN
                    CREATE INDEX [IX_CategoryTranslations_LanguageCode_Name]
                        ON [CategoryTranslations] ([LanguageCode], [Name]);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryTranslations");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
