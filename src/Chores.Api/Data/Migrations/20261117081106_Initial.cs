using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chores.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Areas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Areas_Areas_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Areas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Chores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    Effort = table.Column<int>(type: "INTEGER", nullable: true),
                    AreaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PreferredDay = table.Column<int>(type: "INTEGER", nullable: true),
                    PostponedTo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Interval_Count = table.Column<int>(type: "INTEGER", nullable: true),
                    Interval_Unit = table.Column<int>(type: "INTEGER", nullable: true),
                    Season_FromMonth = table.Column<int>(type: "INTEGER", nullable: true),
                    Season_ToMonth = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chores_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Completions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PostponedTo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Skipped = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChoreId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Completions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Completions_Chores_ChoreId",
                        column: x => x.ChoreId,
                        principalTable: "Chores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Areas_ParentId",
                table: "Areas",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Chores_AreaId",
                table: "Chores",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Completions_ChoreId",
                table: "Completions",
                column: "ChoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Completions");

            migrationBuilder.DropTable(
                name: "Chores");

            migrationBuilder.DropTable(
                name: "Areas");
        }
    }
}
