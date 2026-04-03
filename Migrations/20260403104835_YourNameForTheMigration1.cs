using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatPortal.Migrations
{
    /// <inheritdoc />
    public partial class YourNameForTheMigration1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AIModels",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 10, 48, 35, 379, DateTimeKind.Utc).AddTicks(7816));

            migrationBuilder.UpdateData(
                table: "AIModels",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 10, 48, 35, 379, DateTimeKind.Utc).AddTicks(7823));

            migrationBuilder.UpdateData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 10, 48, 35, 380, DateTimeKind.Utc).AddTicks(2178));

            migrationBuilder.UpdateData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 10, 48, 35, 380, DateTimeKind.Utc).AddTicks(2184));

            migrationBuilder.UpdateData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 10, 48, 35, 380, DateTimeKind.Utc).AddTicks(2185));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 10, 48, 35, 379, DateTimeKind.Utc).AddTicks(7689));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 10, 48, 35, 379, DateTimeKind.Utc).AddTicks(7699));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AIModels",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 2, 23, 12, 23, 468, DateTimeKind.Utc).AddTicks(533));

            migrationBuilder.UpdateData(
                table: "AIModels",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 2, 23, 12, 23, 468, DateTimeKind.Utc).AddTicks(539));

            migrationBuilder.UpdateData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 2, 23, 12, 23, 468, DateTimeKind.Utc).AddTicks(4688));

            migrationBuilder.UpdateData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 2, 23, 12, 23, 468, DateTimeKind.Utc).AddTicks(4697));

            migrationBuilder.UpdateData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 2, 23, 12, 23, 468, DateTimeKind.Utc).AddTicks(4699));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 2, 23, 12, 23, 468, DateTimeKind.Utc).AddTicks(397));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 2, 23, 12, 23, 468, DateTimeKind.Utc).AddTicks(402));
        }
    }
}
