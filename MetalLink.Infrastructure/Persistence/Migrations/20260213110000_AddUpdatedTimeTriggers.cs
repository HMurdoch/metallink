using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations;

public partial class AddUpdatedTimeTriggers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create or replace a generic trigger function that stamps updated_time on every UPDATE.
        migrationBuilder.Sql(@"
            CREATE OR REPLACE FUNCTION metal_link.set_updated_time()
            RETURNS trigger AS $$
            BEGIN
                NEW.updated_time = now();
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
        ");

        // Attach the trigger to every table in schema metal_link that has an updated_time column.
        // This ensures updated_time is set even for raw SQL updates.
        migrationBuilder.Sql(@"
            DO $$
            DECLARE
                r record;
                trig_name text;
            BEGIN
                FOR r IN
                    SELECT c.table_name
                    FROM information_schema.columns c
                    WHERE c.table_schema = 'metal_link'
                      AND c.column_name = 'updated_time'
                LOOP
                    trig_name := 'trg_set_updated_time__' || r.table_name;

                    EXECUTE format('DROP TRIGGER IF EXISTS %I ON metal_link.%I;', trig_name, r.table_name);
                    EXECUTE format('CREATE TRIGGER %I BEFORE UPDATE ON metal_link.%I FOR EACH ROW EXECUTE FUNCTION metal_link.set_updated_time();', trig_name, r.table_name);
                END LOOP;
            END $$;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop triggers
        migrationBuilder.Sql(@"
            DO $$
            DECLARE
                r record;
                trig_name text;
            BEGIN
                FOR r IN
                    SELECT c.table_name
                    FROM information_schema.columns c
                    WHERE c.table_schema = 'metal_link'
                      AND c.column_name = 'updated_time'
                LOOP
                    trig_name := 'trg_set_updated_time__' || r.table_name;
                    EXECUTE format('DROP TRIGGER IF EXISTS %I ON metal_link.%I;', trig_name, r.table_name);
                END LOOP;
            END $$;
        ");

        // Drop function
        migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS metal_link.set_updated_time();");
    }
}
