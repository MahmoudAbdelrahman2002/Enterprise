using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enterprise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleTranslations",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleTranslations", x => new { x.RoleId, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_RoleTranslations_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleTranslations_LanguageCode_Name",
                table: "RoleTranslations",
                columns: new[] { "LanguageCode", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleTranslations");
        }
    }
}
