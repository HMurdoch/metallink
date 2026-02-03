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
            // Add tare column to receiving_ticket_lines table
            migrationBuilder.Sql(
                @"ALTER TABLE metal_link.receiving_ticket_lines 
                  ADD COLUMN tare numeric(18,2) NOT NULL DEFAULT 0.00;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"ALTER TABLE metal_link.receiving_ticket_lines 
                  DROP COLUMN tare;");
        }
    }
}
