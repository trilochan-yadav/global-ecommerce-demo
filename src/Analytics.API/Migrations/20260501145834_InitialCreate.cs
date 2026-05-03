using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Analytics.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClickEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClickEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConversionStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversionStats", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ClickEvents",
                columns: new[] { "Id", "CreatedAt", "EventType", "ProductId", "UserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 4, 1, 9, 0, 0, 0, DateTimeKind.Utc), "view", 1, 1 },
                    { 2, new DateTime(2026, 4, 1, 9, 10, 0, 0, DateTimeKind.Utc), "view", 2, 1 },
                    { 3, new DateTime(2026, 4, 1, 10, 0, 0, 0, DateTimeKind.Utc), "view", 1, 2 },
                    { 4, new DateTime(2026, 4, 1, 10, 15, 0, 0, DateTimeKind.Utc), "view", 3, 2 },
                    { 5, new DateTime(2026, 4, 2, 8, 0, 0, 0, DateTimeKind.Utc), "view", 4, 3 },
                    { 6, new DateTime(2026, 4, 2, 8, 30, 0, 0, DateTimeKind.Utc), "add_cart", 1, 1 },
                    { 7, new DateTime(2026, 4, 2, 9, 0, 0, 0, DateTimeKind.Utc), "add_cart", 3, 2 },
                    { 8, new DateTime(2026, 4, 3, 11, 0, 0, 0, DateTimeKind.Utc), "view", 5, 3 },
                    { 9, new DateTime(2026, 4, 3, 11, 20, 0, 0, DateTimeKind.Utc), "add_cart", 2, 4 },
                    { 10, new DateTime(2026, 4, 3, 12, 0, 0, 0, DateTimeKind.Utc), "view", 6, 4 }
                });

            migrationBuilder.InsertData(
                table: "ConversionStats",
                columns: new[] { "Id", "CreatedAt", "CustomerId", "OrderId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 4, 1, 10, 30, 0, 0, DateTimeKind.Utc), 1, 1 },
                    { 2, new DateTime(2026, 4, 2, 9, 45, 0, 0, DateTimeKind.Utc), 2, 2 },
                    { 3, new DateTime(2026, 4, 3, 13, 0, 0, 0, DateTimeKind.Utc), 3, 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClickEvents");

            migrationBuilder.DropTable(
                name: "ConversionStats");
        }
    }
}
