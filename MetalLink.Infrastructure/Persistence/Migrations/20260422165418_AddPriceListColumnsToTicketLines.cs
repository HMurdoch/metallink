using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceListColumnsToTicketLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "product_price_list_id",
                schema: "metal_link",
                table: "stock_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "product_price_list_product_price_id",
                schema: "metal_link",
                table: "stock_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_product_price_list_id",
                schema: "metal_link",
                table: "stock_movements",
                column: "product_price_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_product_price_list_product_price_id",
                schema: "metal_link",
                table: "stock_movements",
                column: "product_price_list_product_price_id");

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_product_price_lists_product_price_list_id",
                schema: "metal_link",
                table: "stock_movements",
                column: "product_price_list_id",
                principalSchema: "metal_link",
                principalTable: "product_price_lists",
                principalColumn: "product_price_list_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_product_price_list_product_prices_product_price_id",
                schema: "metal_link",
                table: "stock_movements",
                column: "product_price_list_product_price_id",
                principalSchema: "metal_link",
                principalTable: "product_price_list_product_prices",
                principalColumn: "product_price_list_product_price_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddColumn<int>(
                name: "product_price_list_id",
                schema: "metal_link",
                table: "sending_ticket_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "product_price_list_product_price_id",
                schema: "metal_link",
                table: "sending_ticket_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "product_price_list_id",
                schema: "metal_link",
                table: "receiving_ticket_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "product_price_list_product_price_id",
                schema: "metal_link",
                table: "receiving_ticket_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sending_ticket_lines_product_price_list_id",
                schema: "metal_link",
                table: "sending_ticket_lines",
                column: "product_price_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_sending_ticket_lines_product_price_list_product_price_id",
                schema: "metal_link",
                table: "sending_ticket_lines",
                column: "product_price_list_product_price_id");

            migrationBuilder.CreateIndex(
                name: "IX_receiving_ticket_lines_product_price_list_id",
                schema: "metal_link",
                table: "receiving_ticket_lines",
                column: "product_price_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_receiving_ticket_lines_product_price_list_product_price_id",
                schema: "metal_link",
                table: "receiving_ticket_lines",
                column: "product_price_list_product_price_id");

            migrationBuilder.AddForeignKey(
                name: "FK_receiving_ticket_lines_product_price_list_product_prices_pr~",
                schema: "metal_link",
                table: "receiving_ticket_lines",
                column: "product_price_list_product_price_id",
                principalSchema: "metal_link",
                principalTable: "product_price_list_product_prices",
                principalColumn: "product_price_list_product_price_id");

            migrationBuilder.AddForeignKey(
                name: "FK_receiving_ticket_lines_product_price_lists_product_price_li~",
                schema: "metal_link",
                table: "receiving_ticket_lines",
                column: "product_price_list_id",
                principalSchema: "metal_link",
                principalTable: "product_price_lists",
                principalColumn: "product_price_list_id");

            migrationBuilder.AddForeignKey(
                name: "FK_sending_ticket_lines_product_price_list_product_prices_prod~",
                schema: "metal_link",
                table: "sending_ticket_lines",
                column: "product_price_list_product_price_id",
                principalSchema: "metal_link",
                principalTable: "product_price_list_product_prices",
                principalColumn: "product_price_list_product_price_id");

            migrationBuilder.AddForeignKey(
                name: "FK_sending_ticket_lines_product_price_lists_product_price_list~",
                schema: "metal_link",
                table: "sending_ticket_lines",
                column: "product_price_list_id",
                principalSchema: "metal_link",
                principalTable: "product_price_lists",
                principalColumn: "product_price_list_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_receiving_ticket_lines_product_price_list_product_prices_pr~",
                schema: "metal_link",
                table: "receiving_ticket_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_receiving_ticket_lines_product_price_lists_product_price_li~",
                schema: "metal_link",
                table: "receiving_ticket_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_sending_ticket_lines_product_price_list_product_prices_prod~",
                schema: "metal_link",
                table: "sending_ticket_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_sending_ticket_lines_product_price_lists_product_price_list~",
                schema: "metal_link",
                table: "sending_ticket_lines");

            migrationBuilder.DropIndex(
                name: "IX_sending_ticket_lines_product_price_list_id",
                schema: "metal_link",
                table: "sending_ticket_lines");

            migrationBuilder.DropIndex(
                name: "IX_sending_ticket_lines_product_price_list_product_price_id",
                schema: "metal_link",
                table: "sending_ticket_lines");

            migrationBuilder.DropIndex(
                name: "IX_receiving_ticket_lines_product_price_list_id",
                schema: "metal_link",
                table: "receiving_ticket_lines");

            migrationBuilder.DropIndex(
                name: "IX_receiving_ticket_lines_product_price_list_product_price_id",
                schema: "metal_link",
                table: "receiving_ticket_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_product_price_list_id",
                schema: "metal_link",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_product_price_list_product_price_id",
                schema: "metal_link",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_product_price_lists_product_price_list_id",
                schema: "metal_link",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_product_price_list_product_prices_product_price_id",
                schema: "metal_link",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "product_price_list_id",
                schema: "metal_link",
                table: "sending_ticket_lines");

            migrationBuilder.DropColumn(
                name: "product_price_list_product_price_id",
                schema: "metal_link",
                table: "sending_ticket_lines");

            migrationBuilder.DropColumn(
                name: "product_price_list_id",
                schema: "metal_link",
                table: "receiving_ticket_lines");

            migrationBuilder.DropColumn(
                name: "product_price_list_product_price_id",
                schema: "metal_link",
                table: "receiving_ticket_lines");

            migrationBuilder.DropColumn(
                name: "product_price_list_id",
                schema: "metal_link",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "product_price_list_product_price_id",
                schema: "metal_link",
                table: "stock_movements");
        }
    }
}