using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatPortal.Migrations
{
    /// <inheritdoc />
    public partial class YourNameForTheMigration4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Users_InvitedBy",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamWorkspacePermissions_Users_GrantedBy",
                table: "TeamWorkspacePermissions");

            migrationBuilder.UpdateData(
                table: "AIModels",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 26, 26, 718, DateTimeKind.Utc).AddTicks(8434));

            migrationBuilder.UpdateData(
                table: "AIModels",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 26, 26, 718, DateTimeKind.Utc).AddTicks(8442));

            migrationBuilder.UpdateData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 26, 26, 720, DateTimeKind.Utc).AddTicks(955));

            migrationBuilder.UpdateData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 26, 26, 720, DateTimeKind.Utc).AddTicks(963));

            migrationBuilder.UpdateData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 26, 26, 720, DateTimeKind.Utc).AddTicks(965));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 26, 26, 718, DateTimeKind.Utc).AddTicks(8274));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 26, 26, 718, DateTimeKind.Utc).AddTicks(8280));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 26, 26, 718, DateTimeKind.Utc).AddTicks(8281));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 26, 26, 718, DateTimeKind.Utc).AddTicks(8282));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 26, 26, 718, DateTimeKind.Utc).AddTicks(8283));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 26, 26, 718, DateTimeKind.Utc).AddTicks(8283));

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Users_InvitedBy",
                table: "Invitations",
                column: "InvitedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamWorkspacePermissions_Users_GrantedBy",
                table: "TeamWorkspacePermissions",
                column: "GrantedBy",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Users_InvitedBy",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamWorkspacePermissions_Users_GrantedBy",
                table: "TeamWorkspacePermissions");

            migrationBuilder.UpdateData(
                table: "AIModels",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 25, 13, 856, DateTimeKind.Utc).AddTicks(5286));

            migrationBuilder.UpdateData(
                table: "AIModels",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 25, 13, 856, DateTimeKind.Utc).AddTicks(5292));

            migrationBuilder.UpdateData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 25, 13, 857, DateTimeKind.Utc).AddTicks(4933));

            migrationBuilder.UpdateData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 25, 13, 857, DateTimeKind.Utc).AddTicks(4936));

            migrationBuilder.UpdateData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 25, 13, 857, DateTimeKind.Utc).AddTicks(4937));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 25, 13, 856, DateTimeKind.Utc).AddTicks(5126));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 25, 13, 856, DateTimeKind.Utc).AddTicks(5132));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 25, 13, 856, DateTimeKind.Utc).AddTicks(5133));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 25, 13, 856, DateTimeKind.Utc).AddTicks(5134));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 25, 13, 856, DateTimeKind.Utc).AddTicks(5134));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 13, 25, 13, 856, DateTimeKind.Utc).AddTicks(5135));

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Users_InvitedBy",
                table: "Invitations",
                column: "InvitedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamWorkspacePermissions_Users_GrantedBy",
                table: "TeamWorkspacePermissions",
                column: "GrantedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
