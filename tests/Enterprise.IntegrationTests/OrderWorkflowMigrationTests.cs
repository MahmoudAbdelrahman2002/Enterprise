using Enterprise.Infrastructure.Persistence.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Enterprise.IntegrationTests;

public class OrderWorkflowMigrationTests
{
    private sealed class TestMigration : SimplifyOrderWorkflow
    {
        public MigrationBuilder BuildUp() { var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer"); Up(builder); return builder; }
        public MigrationBuilder BuildDown() { var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer"); Down(builder); return builder; }
    }

    [Test]
    public async Task LegacyOrders_MigrateWithoutLosingHistory_AndCanBeRolledBack()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await Execute("CREATE TABLE Orders (Id INTEGER PRIMARY KEY, Status INTEGER NOT NULL, PreviousStatus TEXT NULL)");
        for (var status = 0; status <= 5; status++) await Execute($"INSERT INTO Orders (Id, Status) VALUES ({status}, {status})");
        var migration = new TestMigration();
        foreach (var operation in migration.BuildUp().Operations.OfType<SqlOperation>()) await Execute(operation.Sql);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT Status, PreviousStatus FROM Orders ORDER BY Id";
            await using var reader = await command.ExecuteReaderAsync();
            var expected = new[] { 0, 0, 2, 3, 3, 3 };
            var prior = new string?[] { null, "Accepted", null, null, "Completed", "Cancelled" };
            for (var index = 0; index < expected.Length; index++)
            {
                Assert.That(await reader.ReadAsync(), Is.True);
                Assert.That(reader.GetInt32(0), Is.EqualTo(expected[index]));
                Assert.That(reader.IsDBNull(1) ? null : reader.GetString(1), Is.EqualTo(prior[index]));
            }
        }
        foreach (var operation in migration.BuildDown().Operations.OfType<SqlOperation>()) await Execute(operation.Sql);
        await using var verify = connection.CreateCommand();
        verify.CommandText = "SELECT COUNT(*) FROM Orders WHERE Status = Id";
        Assert.That(Convert.ToInt32(await verify.ExecuteScalarAsync()), Is.EqualTo(6));

        async Task Execute(string sql) { await using var command = connection.CreateCommand(); command.CommandText = sql; await command.ExecuteNonQueryAsync(); }
    }
}
