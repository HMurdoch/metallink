using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Database drift hotfix migration:
    /// - Drop legacy column metal_link.sending_tickets.initialize_weight (we use initialize_weight_kg)
    /// - Add notes column to metal_link.stock_movements
    ///
    /// Uses raw SQL so it can run even if EF model does not track these legacy columns/tables.
    /// </summary>
    public partial class DropLegacyInitializeWeightAndAddStockMovementNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'metal_link'
          AND table_name = 'sending_tickets'
          AND column_name = 'initialize_weight'
    ) THEN
        ALTER TABLE metal_link.sending_tickets DROP COLUMN initialize_weight;
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'metal_link'
          AND table_name = 'stock_movements'
    ) THEN
        IF NOT EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'metal_link'
              AND table_name = 'stock_movements'
              AND column_name = 'notes'
        ) THEN
            ALTER TABLE metal_link.stock_movements ADD COLUMN notes text;
        END IF;
    END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort reverse.
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'metal_link'
          AND table_name = 'sending_tickets'
    ) THEN
        IF NOT EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'metal_link'
              AND table_name = 'sending_tickets'
              AND column_name = 'initialize_weight'
        ) THEN
            ALTER TABLE metal_link.sending_tickets ADD COLUMN initialize_weight numeric(18,2);
        END IF;
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'metal_link'
          AND table_name = 'stock_movements'
          AND column_name = 'notes'
    ) THEN
        ALTER TABLE metal_link.stock_movements DROP COLUMN notes;
    END IF;
END $$;
");
        }
    }
}
