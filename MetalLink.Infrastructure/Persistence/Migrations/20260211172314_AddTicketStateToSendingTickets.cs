using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddTicketStateToSendingTickets : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ticket_state",
            schema: "metal_link",
            table: "sending_tickets",
            type: "character varying(1)",
            maxLength: 1,
            nullable: false,
            defaultValue: "H");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ticket_state",
            schema: "metal_link",
            table: "sending_tickets");
    }
}
