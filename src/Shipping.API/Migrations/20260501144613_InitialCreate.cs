using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Shipping.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    TrackingNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipments", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Shipments",
                columns: new[] { "Id", "CreatedAt", "OrderId", "Status", "TrackingNumber" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 4, 1, 12, 0, 0, 0, DateTimeKind.Utc), 1, 2, "TRK-100001" },
                    { 2, new DateTime(2026, 4, 2, 13, 0, 0, 0, DateTimeKind.Utc), 2, 1, "TRK-100002" },
                    { 3, new DateTime(2026, 4, 3, 14, 0, 0, 0, DateTimeKind.Utc), 3, 0, "TRK-100003" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Shipments");
        }
    }
}
