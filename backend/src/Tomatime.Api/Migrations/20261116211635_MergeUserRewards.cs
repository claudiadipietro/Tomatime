using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tomatime.Api.Migrations
{
    /// <inheritdoc />
    public partial class MergeUserRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRewards_Rewards_RewardId",
                table: "UserRewards");

            migrationBuilder.DropIndex(
                name: "IX_UserRewards_UserId_RewardId",
                table: "UserRewards");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UserRewards",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "UserRewards",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OperationId",
                table: "UserRewards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "UserRewards",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM "RewardOperations"
                        WHERE "RewardId" <> -3
                        GROUP BY "UserId", "RewardId"
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Cannot merge multiple operations for the same non-repeatable reward; review the data first.';
                    END IF;
                END $$;

                UPDATE "UserRewards" AS ur
                SET "OperationId" = ro."Id", "Kind" = ro."Kind",
                    "SessionId" = ro."SessionId", "CreatedAt" = ro."CreatedAt"
                FROM "RewardOperations" AS ro
                WHERE ur."UserId" = ro."UserId" AND ur."RewardId" = ro."RewardId"
                  AND ro."RewardId" <> -3;

                INSERT INTO "UserRewards" ("UserId", "RewardId", "OperationId", "Kind", "SessionId", "CreatedAt")
                SELECT ro."UserId", ro."RewardId", ro."Id", ro."Kind", ro."SessionId", ro."CreatedAt"
                FROM "RewardOperations" AS ro
                WHERE NOT EXISTS (SELECT 1 FROM "UserRewards" AS ur WHERE ur."OperationId" = ro."Id");
                """);

            migrationBuilder.DropTable(
                name: "RewardOperations");

            migrationBuilder.CreateIndex(
                name: "IX_UserRewards_OperationId",
                table: "UserRewards",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRewards_UserId_RewardId",
                table: "UserRewards",
                columns: new[] { "UserId", "RewardId" },
                unique: true,
                filter: "\"RewardId\" <> -3");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRewards_Rewards_RewardId",
                table: "UserRewards",
                column: "RewardId",
                principalTable: "Rewards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRewards_Rewards_RewardId",
                table: "UserRewards");

            migrationBuilder.DropIndex(
                name: "IX_UserRewards_OperationId",
                table: "UserRewards");

            migrationBuilder.DropIndex(
                name: "IX_UserRewards_UserId_RewardId",
                table: "UserRewards");

            migrationBuilder.CreateTable(
                name: "RewardOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RewardId = table.Column<int>(type: "integer", nullable: false),
                    SessionId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM "UserRewards"
                        WHERE "OperationId" IS NOT NULL AND ("Kind" IS NULL OR "CreatedAt" IS NULL)
                    ) THEN
                        RAISE EXCEPTION 'Cannot restore an operation without its kind and creation time.';
                    END IF;
                END $$;

                INSERT INTO "RewardOperations" ("Id", "UserId", "RewardId", "Kind", "SessionId", "CreatedAt")
                SELECT "OperationId", "UserId", "RewardId", "Kind", "SessionId", "CreatedAt"
                FROM "UserRewards" WHERE "OperationId" IS NOT NULL;

                DELETE FROM "UserRewards" WHERE "RewardId" = -3 AND "OperationId" IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserRewards");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "UserRewards");

            migrationBuilder.DropColumn(
                name: "OperationId",
                table: "UserRewards");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "UserRewards");

            migrationBuilder.CreateIndex(
                name: "IX_UserRewards_UserId_RewardId",
                table: "UserRewards",
                columns: new[] { "UserId", "RewardId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RewardOperations_RewardId",
                table: "RewardOperations",
                column: "RewardId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardOperations_UserId",
                table: "RewardOperations",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRewards_Rewards_RewardId",
                table: "UserRewards",
                column: "RewardId",
                principalTable: "Rewards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
