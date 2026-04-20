using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations;

public partial class AddTicketNumberSequences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create sequences (one per prefix) and initialize them to MAX(existing)+1 across BOTH active and inactive rows.
        // We keep them in the metal_link schema.

        migrationBuilder.Sql(@"
DO $$
BEGIN
    -- Create sequences if missing
    IF NOT EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='metal_link' AND c.relkind='S' AND c.relname='ticket_number_rwb_seq') THEN
        CREATE SEQUENCE metal_link.ticket_number_rwb_seq;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='metal_link' AND c.relkind='S' AND c.relname='ticket_number_rpl_seq') THEN
        CREATE SEQUENCE metal_link.ticket_number_rpl_seq;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='metal_link' AND c.relkind='S' AND c.relname='ticket_number_swb_seq') THEN
        CREATE SEQUENCE metal_link.ticket_number_swb_seq;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='metal_link' AND c.relkind='S' AND c.relname='ticket_number_spl_seq') THEN
        CREATE SEQUENCE metal_link.ticket_number_spl_seq;
    END IF;

    -- Initialize sequences to max numeric part + 1
    PERFORM setval('metal_link.ticket_number_rwb_seq', COALESCE((
        SELECT MAX(CAST(SUBSTRING(ticket_number FROM 5) AS INTEGER))
        FROM metal_link.receiving_tickets
        WHERE ticket_number LIKE 'RWB-%'
    ), 0) + 1, false);

    PERFORM setval('metal_link.ticket_number_rpl_seq', COALESCE((
        SELECT MAX(CAST(SUBSTRING(ticket_number FROM 5) AS INTEGER))
        FROM metal_link.receiving_tickets
        WHERE ticket_number LIKE 'RPL-%'
    ), 0) + 1, false);

    PERFORM setval('metal_link.ticket_number_swb_seq', COALESCE((
        SELECT MAX(CAST(SUBSTRING(ticket_number FROM 5) AS INTEGER))
        FROM metal_link.sending_tickets
        WHERE ticket_number LIKE 'SWB-%'
    ), 0) + 1, false);

    PERFORM setval('metal_link.ticket_number_spl_seq', COALESCE((
        SELECT MAX(CAST(SUBSTRING(ticket_number FROM 5) AS INTEGER))
        FROM metal_link.sending_tickets
        WHERE ticket_number LIKE 'SPL-%'
    ), 0) + 1, false);
END $$;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP SEQUENCE IF EXISTS metal_link.ticket_number_rwb_seq;
DROP SEQUENCE IF EXISTS metal_link.ticket_number_rpl_seq;
DROP SEQUENCE IF EXISTS metal_link.ticket_number_swb_seq;
DROP SEQUENCE IF EXISTS metal_link.ticket_number_spl_seq;
");
    }
}
