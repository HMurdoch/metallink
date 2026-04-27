using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPriceListProductPriceIdToStockLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE metal_link.stock_levels
                ADD COLUMN IF NOT EXISTS product_price_list_product_price_id INTEGER;

                ALTER TABLE metal_link.stock_levels
                ADD CONSTRAINT fk_stock_levels_product_price_list_product_price
                FOREIGN KEY (product_price_list_product_price_id)
                REFERENCES metal_link.product_price_list_product_prices(product_price_list_product_price_id)
                ON DELETE SET NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE metal_link.stock_levels
                DROP CONSTRAINT IF EXISTS fk_stock_levels_product_price_list_product_price;

                ALTER TABLE metal_link.stock_levels
                DROP COLUMN IF EXISTS product_price_list_product_price_id;
            ");
        }
    }
}