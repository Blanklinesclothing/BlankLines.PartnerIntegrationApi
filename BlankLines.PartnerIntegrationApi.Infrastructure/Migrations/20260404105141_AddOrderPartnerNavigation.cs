using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlankLines.PartnerIntegrationApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPartnerNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Partners_PartnerId",
                table: "Orders",
                column: "PartnerId",
                principalTable: "Partners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Partners_PartnerId",
                table: "Orders");
        }
    }
}
