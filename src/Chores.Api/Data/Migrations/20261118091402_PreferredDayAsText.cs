using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chores.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PreferredDayAsText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PreferredDay",
                table: "Chores",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            // Weekdays were stored as DayOfWeek numbers, first and last of month as 7 and 8
            migrationBuilder.Sql("""
                UPDATE Chores SET PreferredDay = CASE CAST(PreferredDay AS TEXT)
                    WHEN '0' THEN 'Sunday'
                    WHEN '1' THEN 'Monday'
                    WHEN '2' THEN 'Tuesday'
                    WHEN '3' THEN 'Wednesday'
                    WHEN '4' THEN 'Thursday'
                    WHEN '5' THEN 'Friday'
                    WHEN '6' THEN 'Saturday'
                    WHEN '7' THEN 'FirstOfMonth'
                    WHEN '8' THEN 'LastOfMonth'
                END
                WHERE PreferredDay IS NOT NULL
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Chores SET PreferredDay = CASE PreferredDay
                    WHEN 'Sunday' THEN 0
                    WHEN 'Monday' THEN 1
                    WHEN 'Tuesday' THEN 2
                    WHEN 'Wednesday' THEN 3
                    WHEN 'Thursday' THEN 4
                    WHEN 'Friday' THEN 5
                    WHEN 'Saturday' THEN 6
                    WHEN 'FirstOfMonth' THEN 7
                    WHEN 'LastOfMonth' THEN 8
                END
                WHERE PreferredDay IS NOT NULL
                """);

            migrationBuilder.AlterColumn<int>(
                name: "PreferredDay",
                table: "Chores",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
