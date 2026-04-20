using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations;

public partial class DropSendingTicketHeaderWeightColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE IF EXISTS metal_link.sending_tickets
                DROP COLUMN IF EXISTS first_weight_kg,
                DROP COLUMN IF EXISTS second_weight_kg;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Restore columns as nullable numeric(18,2) to match historical schema.
        migrationBuilder.Sql(@"
            ALTER TABLE IF EXISTS metal_link.sending_tickets
                ADD COLUMN IF NOT EXISTS first_weight_kg numeric(18,2) NULL,
                ADD COLUMN IF NOT EXISTS second_weight_kg numeric(18,2) NULL;
        ");
    }
}
