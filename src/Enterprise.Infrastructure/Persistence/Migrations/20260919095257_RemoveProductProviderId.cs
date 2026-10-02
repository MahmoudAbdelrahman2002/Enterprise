using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enterprise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProductProviderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_ProviderId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProviderId_Sku",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId_Sku",
                table: "Products",
                columns: new[] { "CategoryId", "Sku" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId_Sku",
                table: "Products");

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProviderId",
                table: "Products",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProviderId_Sku",
                table: "Products",
                columns: new[] { "ProviderId", "Sku" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
