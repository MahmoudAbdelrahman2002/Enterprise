using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enterprise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceServicesAndProviderRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceId",
                table: "Providers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MarketplaceServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceServices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceServiceTranslations",
                columns: table => new
                {
                    MarketplaceServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceServiceTranslations", x => new { x.MarketplaceServiceId, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_MarketplaceServiceTranslations_MarketplaceServices_MarketplaceServiceId",
                        column: x => x.MarketplaceServiceId,
                        principalTable: "MarketplaceServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_ServiceId",
                table: "Providers",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceServices_Code",
                table: "MarketplaceServices",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceServices_DisplayOrder",
                table: "MarketplaceServices",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceServices_IsActive",
                table: "MarketplaceServices",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceServiceTranslations_LanguageCode_Name",
                table: "MarketplaceServiceTranslations",
                columns: new[] { "LanguageCode", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_Providers_MarketplaceServices_ServiceId",
                table: "Providers",
                column: "ServiceId",
                principalTable: "MarketplaceServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Providers_MarketplaceServices_ServiceId",
                table: "Providers");

            migrationBuilder.DropTable(
                name: "MarketplaceServiceTranslations");

            migrationBuilder.DropTable(
                name: "MarketplaceServices");

            migrationBuilder.DropIndex(
                name: "IX_Providers_ServiceId",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "Providers");
        }
    }
}
