using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enterprise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyOrderWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreviousStatus",
                table: "Orders",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            // Preserve the old status for history/rollback. Existing cancelled orders remain
            // read-only historical records and are excluded from the active Provider queue.
            migrationBuilder.Sql("""
                UPDATE Orders
                SET PreviousStatus = CASE Status
                    WHEN 1 THEN 'Accepted'
                    WHEN 4 THEN 'Completed'
                    WHEN 5 THEN 'Cancelled'
                    ELSE PreviousStatus END,
                    Status = CASE Status
                    WHEN 1 THEN 0
                    WHEN 4 THEN 3
                    WHEN 5 THEN 3
                    ELSE Status END
                WHERE Status IN (1, 4, 5);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Orders SET Status = CASE
                    WHEN PreviousStatus = 'Accepted' AND Status = 0 THEN 1
                    WHEN PreviousStatus = 'Completed' AND Status = 3 THEN 4
                    WHEN PreviousStatus = 'Cancelled' THEN 5
                    ELSE Status END
                WHERE PreviousStatus IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "PreviousStatus",
                table: "Orders");
        }
    }
}
