using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enterprise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemapOrderStatusToRestaurantFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Old: Pending=0, Processing=1, Shipped=2, Delivered=3, Cancelled=4
            // New: Pending=0, Accepted=1, Preparing=2, Ready=3, Completed=4, Cancelled=5
            migrationBuilder.Sql("""
                UPDATE Orders SET Status = 5 WHERE Status = 4;
                UPDATE Orders SET Status = 4 WHERE Status = 3;
                UPDATE Orders SET Status = 3 WHERE Status = 2;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Orders SET Status = 2 WHERE Status = 3;
                UPDATE Orders SET Status = 3 WHERE Status = 4;
                UPDATE Orders SET Status = 4 WHERE Status = 5;
                """);
        }
    }
}
