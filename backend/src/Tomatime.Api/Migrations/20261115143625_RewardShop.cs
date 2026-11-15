using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Tomatime.Api.Migrations
{
    /// <inheritdoc />
    public partial class RewardShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int[]>(
                name: "BreakExtensions",
                table: "Sessions",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<bool>(
                name: "StoredBreakApplied",
                table: "Sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "RewardOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RewardId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SessionId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RewardOperations_Rewards_RewardId",
                        column: x => x.RewardId,
                        principalTable: "Rewards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RewardOperations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Rewards",
                columns: new[] { "Id", "Cost", "Description", "Name", "ThemeId", "Type" },
                values: new object[] { -3, 5, "Five extra minutes of break", "extra-break", null, "ExtraBreak" });

            migrationBuilder.InsertData(
                table: "Themes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { -2, "blue" },
                    { -1, "pink" }
                });

            migrationBuilder.InsertData(
                table: "Rewards",
                columns: new[] { "Id", "Cost", "Description", "Name", "ThemeId", "Type" },
                values: new object[,]
                {
                    { -2, 5, "Blue theme", "blue", -2, "Theme" },
                    { -1, 5, "Pink theme", "pink", -1, "Theme" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RewardOperations_RewardId",
                table: "RewardOperations",
                column: "RewardId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardOperations_UserId",
                table: "RewardOperations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RewardOperations");

            migrationBuilder.DeleteData(
                table: "Rewards",
                keyColumn: "Id",
                keyValue: -3);

            migrationBuilder.DeleteData(
                table: "Rewards",
                keyColumn: "Id",
                keyValue: -2);

            migrationBuilder.DeleteData(
                table: "Rewards",
                keyColumn: "Id",
                keyValue: -1);

            migrationBuilder.DeleteData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: -2);

            migrationBuilder.DeleteData(
                table: "Themes",
                keyColumn: "Id",
                keyValue: -1);

            migrationBuilder.DropColumn(
                name: "BreakExtensions",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "StoredBreakApplied",
                table: "Sessions");
        }
    }
}
