using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enterprise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesAndUsersSystemScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                table: "AspNetRoles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoleType",
                table: "AspNetRoles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_ProviderId",
                table: "AspNetRoles",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_RoleType_ProviderId",
                table: "AspNetRoles",
                columns: new[] { "RoleType", "ProviderId" });

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoles_Providers_ProviderId",
                table: "AspNetRoles",
                column: "ProviderId",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoles_Providers_ProviderId",
                table: "AspNetRoles");

            migrationBuilder.DropIndex(
                name: "IX_AspNetRoles_ProviderId",
                table: "AspNetRoles");

            migrationBuilder.DropIndex(
                name: "IX_AspNetRoles_RoleType_ProviderId",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "IsSystem",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "RoleType",
                table: "AspNetRoles");
        }
    }
}
