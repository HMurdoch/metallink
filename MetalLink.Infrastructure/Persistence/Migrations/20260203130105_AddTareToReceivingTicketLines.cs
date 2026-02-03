using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTareToReceivingTicketLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "tare",
                table: "receiving_ticket_lines",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Tare weight (material to be deducted from first weight)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tare",
                table: "receiving_ticket_lines");
        }
    }
}
