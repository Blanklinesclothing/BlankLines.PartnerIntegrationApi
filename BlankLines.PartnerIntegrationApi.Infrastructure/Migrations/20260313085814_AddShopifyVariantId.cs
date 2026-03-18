using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlankLines.PartnerIntegrationApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShopifyVariantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ShopifyVariantId",
                table: "PartnerProducts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ShopifyVariantId",
                table: "OrderItems",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShopifyVariantId",
                table: "PartnerProducts");

            migrationBuilder.DropColumn(
                name: "ShopifyVariantId",
                table: "OrderItems");
        }
    }
}
