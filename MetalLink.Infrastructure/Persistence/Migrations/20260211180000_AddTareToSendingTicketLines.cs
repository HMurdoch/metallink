using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddTareToSendingTicketLines : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "tare",
            schema: "metal_link",
            table: "sending_ticket_lines",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "tare",
            schema: "metal_link",
            table: "sending_ticket_lines");
    }
}
