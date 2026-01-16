using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameAndRefactorReceivingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep table names as is for now - will be renamed later
            // Rename foreign key in tickets_receiving_lines
            migrationBuilder.RenameIndex(
                name: "IX_tickets_receiving_lines_ticket_receiving_id",
                table: "tickets_receiving_lines",
                newName: "IX_tickets_receiving_lines_ticket_receiving_id_new");

            // Remove columns from tickets_receiving
            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "tickets_receiving");

            migrationBuilder.DropColumn(
                name: "delivery_status",
                table: "tickets_receiving");

            migrationBuilder.DropColumn(
                name: "load_photo_url",
                table: "tickets_receiving");

            migrationBuilder.DropColumn(
                name: "plate_photo_url",
                table: "tickets_receiving");

            migrationBuilder.DropColumn(
                name: "product_description",
                table: "tickets_receiving");

            migrationBuilder.DropColumn(
                name: "product_id",
                table: "tickets_receiving");

            migrationBuilder.DropColumn(
                name: "rfid_first_scan",
                table: "tickets_receiving");

            migrationBuilder.DropColumn(
                name: "rfid_second_scan",
                table: "tickets_receiving");

            migrationBuilder.DropColumn(
                name: "rfid_tag",
                table: "tickets_receiving");

            migrationBuilder.DropColumn(
                name: "total_amount",
                table: "tickets_receiving");

            migrationBuilder.DropColumn(
                name: "unit_price_per_kg",
                table: "tickets_receiving");

            migrationBuilder.DropColumn(
                name: "updated_by_operator_id",
                table: "tickets_receiving");

            // Add invoice_number column
            migrationBuilder.AddColumn<int>(
                name: "invoice_number",
                table: "tickets_receiving",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Create sequence for invoice_number
            migrationBuilder.Sql("CREATE SEQUENCE invoice_number_seq START 1;");
            migrationBuilder.Sql("ALTER TABLE tickets_receiving ALTER COLUMN invoice_number SET DEFAULT nextval('invoice_number_seq');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop sequence
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS invoice_number_seq;");

            // Remove invoice_number column
            migrationBuilder.DropColumn(
                name: "invoice_number",
                table: "tickets_receiving");

            // Add back removed columns
            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "tickets_receiving",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_status",
                table: "tickets_receiving",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "load_photo_url",
                table: "tickets_receiving",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "plate_photo_url",
                table: "tickets_receiving",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "product_description",
                table: "tickets_receiving",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "product_id",
                table: "tickets_receiving",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rfid_first_scan",
                table: "tickets_receiving",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rfid_second_scan",
                table: "tickets_receiving",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rfid_tag",
                table: "tickets_receiving",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "total_amount",
                table: "tickets_receiving",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_price_per_kg",
                table: "tickets_receiving",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "updated_by_operator_id",
                table: "tickets_receiving",
                type: "bigint",
                nullable: true);

            // Rename index back
            migrationBuilder.RenameIndex(
                name: "IX_tickets_receiving_lines_ticket_receiving_id_new",
                table: "tickets_receiving_lines",
                newName: "IX_tickets_receiving_lines_ticket_receiving_id");
        }
    }
}
