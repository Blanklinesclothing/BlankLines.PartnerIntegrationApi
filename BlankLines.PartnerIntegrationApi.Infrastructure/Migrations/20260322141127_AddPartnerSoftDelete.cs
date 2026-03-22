using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlankLines.PartnerIntegrationApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRevoked",
                table: "Partners",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "Partners",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRevoked",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "Partners");
        }
    }
}
