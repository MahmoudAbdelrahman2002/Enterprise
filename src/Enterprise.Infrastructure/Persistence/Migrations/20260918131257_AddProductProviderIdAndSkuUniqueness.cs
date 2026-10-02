using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enterprise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductProviderIdAndSkuUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Sku",
                table: "Products");

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE p
                SET p.ProviderId = c.ProviderId
                FROM Products p
                INNER JOIN Categories c ON c.Id = p.CategoryId;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProviderId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "IX_Products_Sku",
                table: "Products",
                column: "Sku",
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
