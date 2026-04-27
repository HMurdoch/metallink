using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketReferenceColumnsToStockMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "receiving_ticket_id",
                schema: "metal_link",
                table: "stock_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "receiving_ticket_line_id",
                schema: "metal_link",
                table: "stock_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sending_ticket_id",
                schema: "metal_link",
                table: "stock_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sending_ticket_line_id",
                schema: "metal_link",
                table: "stock_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_receiving_ticket_id",
                schema: "metal_link",
                table: "stock_movements",
                column: "receiving_ticket_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_receiving_ticket_line_id",
                schema: "metal_link",
                table: "stock_movements",
                column: "receiving_ticket_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_sending_ticket_id",
                schema: "metal_link",
                table: "stock_movements",
                column: "sending_ticket_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_sending_ticket_line_id",
                schema: "metal_link",
                table: "stock_movements",
                column: "sending_ticket_line_id");

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_receiving_tickets_receiving_ticket_id",
                schema: "metal_link",
                table: "stock_movements",
                column: "receiving_ticket_id",
                principalSchema: "metal_link",
                principalTable: "receiving_tickets",
                principalColumn: "receiving_ticket_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_receiving_ticket_lines_receiving_ticket_line_id",
                schema: "metal_link",
                table: "stock_movements",
                column: "receiving_ticket_line_id",
                principalSchema: "metal_link",
                principalTable: "receiving_ticket_lines",
                principalColumn: "receiving_ticket_line_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_sending_tickets_sending_ticket_id",
                schema: "metal_link",
                table: "stock_movements",
                column: "sending_ticket_id",
                principalSchema: "metal_link",
                principalTable: "sending_tickets",
                principalColumn: "sending_ticket_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_sending_ticket_lines_sending_ticket_line_id",
                schema: "metal_link",
                table: "stock_movements",
                column: "sending_ticket_line_id",
                principalSchema: "metal_link",
                principalTable: "sending_ticket_lines",
                principalColumn: "sending_ticket_line_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_receiving_tickets_receiving_ticket_id",
                schema: "metal_link",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_receiving_ticket_lines_receiving_ticket_line_id",
                schema: "metal_link",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_sending_tickets_sending_ticket_id",
                schema: "metal_link",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_sending_ticket_lines_sending_ticket_line_id",
                schema: "metal_link",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_receiving_ticket_id",
                schema: "metal_link",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_receiving_ticket_line_id",
                schema: "metal_link",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_sending_ticket_id",
                schema: "metal_link",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_sending_ticket_line_id",
                schema: "metal_link",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "receiving_ticket_id",
                schema: "metal_link",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "receiving_ticket_line_id",
                schema: "metal_link",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "sending_ticket_id",
                schema: "metal_link",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "sending_ticket_line_id",
                schema: "metal_link",
                table: "stock_movements");
        }
    }
}
