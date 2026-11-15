using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tomatime.Api.Migrations
{
    /// <inheritdoc />
    public partial class SessionEndTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAt",
                table: "Sessions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "Sessions");
        }
    }
}
