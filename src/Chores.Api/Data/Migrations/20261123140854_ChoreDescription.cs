using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chores.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChoreDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Chores",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Chores");
        }
    }
}
