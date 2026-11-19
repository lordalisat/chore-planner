using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chores.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ArchivedOn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ArchivedOn",
                table: "Chores",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedOn",
                table: "Chores");
        }
    }
}
