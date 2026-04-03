using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ChatPortal.Migrations
{
    /// <inheritdoc />
    public partial class YourNameForTheMigration2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkspaceId",
                table: "ChatSessions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FeatureToggles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AllowedRoles = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TeamId = table.Column<int>(type: "int", nullable: true),
                    CreatedByAdminId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureToggles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureToggles_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FeatureToggles_Users_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: true),
                    ChatAgentContext = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workspaces_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Workspaces_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "AIModels",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 11, 53, 34, 565, DateTimeKind.Utc).AddTicks(1373));

            migrationBuilder.UpdateData(
                table: "AIModels",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 11, 53, 34, 565, DateTimeKind.Utc).AddTicks(1380));

            migrationBuilder.UpdateData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 11, 53, 34, 565, DateTimeKind.Utc).AddTicks(7009));

            migrationBuilder.UpdateData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 11, 53, 34, 565, DateTimeKind.Utc).AddTicks(7012));

            migrationBuilder.UpdateData(
                table: "CreditPackages",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 11, 53, 34, 565, DateTimeKind.Utc).AddTicks(7013));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 11, 53, 34, 565, DateTimeKind.Utc).AddTicks(1233));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 4, 3, 11, 53, 34, 565, DateTimeKind.Utc).AddTicks(1242));

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "Permissions" },
                values: new object[,]
                {
                    { 3, new DateTime(2026, 4, 3, 11, 53, 34, 565, DateTimeKind.Utc).AddTicks(1243), "Read-only access to dashboards and reports", "Viewer", null },
                    { 4, new DateTime(2026, 4, 3, 11, 53, 34, 565, DateTimeKind.Utc).AddTicks(1244), "Access to chat, tasks, and shared workspaces", "Member", null },
                    { 5, new DateTime(2026, 4, 3, 11, 53, 34, 565, DateTimeKind.Utc).AddTicks(1245), "Can create and edit content in workspaces", "Contributor", null },
                    { 6, new DateTime(2026, 4, 3, 11, 53, 34, 565, DateTimeKind.Utc).AddTicks(1245), "Full tenant-wide access including billing and feature toggles", "Super Admin", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_WorkspaceId",
                table: "ChatSessions",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureToggles_CreatedByAdminId",
                table: "FeatureToggles",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureToggles_TeamId",
                table: "FeatureToggles",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_OwnerId",
                table: "Workspaces",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_TeamId",
                table: "Workspaces",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatSessions_Workspaces_WorkspaceId",
                table: "ChatSessions",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatSessions_Workspaces_WorkspaceId",
                table: "ChatSessions");

            migrationBuilder.DropTable(
                name: "FeatureToggles");

            migrationBuilder.DropTable(
                name: "Workspaces");

            migrationBuilder.DropIndex(
                name: "IX_ChatSessions_WorkspaceId",
                table: "ChatSessions");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "ChatSessions");

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
    }
}
