using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketReceivingAndSendingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create tickets_receiving table
            migrationBuilder.CreateTable(
                name: "tickets_receiving",
                schema: "metal_link",
                columns: table => new
                {
                    ticket_receiving_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    site_id = table.Column<long>(type: "bigint", nullable: false),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    ticket_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ticket_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "weighbridge"),
                    first_weight_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    second_weight_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    net_weight_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    unit_price_per_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "ZAR"),
                    product_id = table.Column<long>(type: "bigint", nullable: true),
                    product_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    vehicle_registration = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    trailer_registration = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    driver_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ofm_weighbridge_ticket = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    foreign_ticket = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ck_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    delivery_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    rfid_tag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    rfid_first_scan = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rfid_second_scan = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    delivery_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "pending"),
                    notes = table.Column<string>(type: "text", nullable: true),
                    plate_photo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    load_photo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_operator_id = table.Column<long>(type: "bigint", nullable: false),
                    updated_by_operator_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tickets_receiving", x => x.ticket_receiving_id);
                    table.ForeignKey(
                        name: "fk_tickets_receiving_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "metal_link",
                        principalTable: "companies",
                        principalColumn: "company_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tickets_receiving_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "metal_link",
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tickets_receiving_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "metal_link",
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tickets_receiving_sites_site_id",
                        column: x => x.site_id,
                        principalSchema: "metal_link",
                        principalTable: "sites",
                        principalColumn: "site_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tickets_sending",
                schema: "metal_link",
                columns: table => new
                {
                    ticket_sending_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    site_id = table.Column<long>(type: "bigint", nullable: false),
                    buyer_id = table.Column<long>(type: "bigint", nullable: false),
                    ticket_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ticket_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "weighbridge"),
                    first_weight_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    second_weight_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    net_weight_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    unit_price_per_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "ZAR"),
                    product_id = table.Column<long>(type: "bigint", nullable: true),
                    product_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    vehicle_registration = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    trailer_registration = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    driver_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ofm_weighbridge_ticket = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    foreign_ticket = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ck_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    delivery_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    rfid_tag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    rfid_first_scan = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rfid_second_scan = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    delivery_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "pending"),
                    notes = table.Column<string>(type: "text", nullable: true),
                    plate_photo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    load_photo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_operator_id = table.Column<long>(type: "bigint", nullable: false),
                    updated_by_operator_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tickets_sending", x => x.ticket_sending_id);
                    table.ForeignKey(
                        name: "fk_tickets_sending_buyers_buyer_id",
                        column: x => x.buyer_id,
                        principalSchema: "metal_link",
                        principalTable: "buyers",
                        principalColumn: "buyer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tickets_sending_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "metal_link",
                        principalTable: "companies",
                        principalColumn: "company_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tickets_sending_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "metal_link",
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tickets_sending_sites_site_id",
                        column: x => x.site_id,
                        principalSchema: "metal_link",
                        principalTable: "sites",
                        principalColumn: "site_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ticket_receiving_lines",
                schema: "metal_link",
                columns: table => new
                {
                    ticket_receiving_line_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    ticket_receiving_id = table.Column<long>(type: "bigint", nullable: false),
                    unit_price_per_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    weight_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticket_receiving_lines", x => x.ticket_receiving_line_id);
                    table.ForeignKey(
                        name: "fk_ticket_receiving_lines_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "metal_link",
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ticket_receiving_lines_tickets_receiving_ticket_receiving_id",
                        column: x => x.ticket_receiving_id,
                        principalSchema: "metal_link",
                        principalTable: "tickets_receiving",
                        principalColumn: "ticket_receiving_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ticket_sending_lines",
                schema: "metal_link",
                columns: table => new
                {
                    ticket_sending_line_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    ticket_sending_id = table.Column<long>(type: "bigint", nullable: false),
                    unit_price_per_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    weight_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticket_sending_lines", x => x.ticket_sending_line_id);
                    table.ForeignKey(
                        name: "fk_ticket_sending_lines_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "metal_link",
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ticket_sending_lines_tickets_sending_ticket_sending_id",
                        column: x => x.ticket_sending_id,
                        principalSchema: "metal_link",
                        principalTable: "tickets_sending",
                        principalColumn: "ticket_sending_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "pk_tickets_receiving",
                schema: "metal_link",
                table: "tickets_receiving",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "tickets_receiving_created_time_idx",
                schema: "metal_link",
                table: "tickets_receiving",
                column: "created_time");

            migrationBuilder.CreateIndex(
                name: "tickets_receiving_customer_id_idx",
                schema: "metal_link",
                table: "tickets_receiving",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_tickets_receiving_product_id",
                schema: "metal_link",
                table: "tickets_receiving",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "tickets_receiving_site_id_idx",
                schema: "metal_link",
                table: "tickets_receiving",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "tickets_receiving_ticket_number_idx",
                schema: "metal_link",
                table: "tickets_receiving",
                column: "ticket_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticket_receiving_lines_product_id",
                schema: "metal_link",
                table: "ticket_receiving_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_ticket_receiving_lines_ticket_receiving_id",
                schema: "metal_link",
                table: "ticket_receiving_lines",
                column: "ticket_receiving_id");

            migrationBuilder.CreateIndex(
                name: "pk_tickets_sending",
                schema: "metal_link",
                table: "tickets_sending",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_tickets_sending_buyer_id",
                schema: "metal_link",
                table: "tickets_sending",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "tickets_sending_created_time_idx",
                schema: "metal_link",
                table: "tickets_sending",
                column: "created_time");

            migrationBuilder.CreateIndex(
                name: "ix_tickets_sending_product_id",
                schema: "metal_link",
                table: "tickets_sending",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "tickets_sending_site_id_idx",
                schema: "metal_link",
                table: "tickets_sending",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "tickets_sending_ticket_number_idx",
                schema: "metal_link",
                table: "tickets_sending",
                column: "ticket_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticket_sending_lines_product_id",
                schema: "metal_link",
                table: "ticket_sending_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_ticket_sending_lines_ticket_sending_id",
                schema: "metal_link",
                table: "ticket_sending_lines",
                column: "ticket_sending_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticket_receiving_lines",
                schema: "metal_link");

            migrationBuilder.DropTable(
                name: "ticket_sending_lines",
                schema: "metal_link");

            migrationBuilder.DropTable(
                name: "tickets_receiving",
                schema: "metal_link");

            migrationBuilder.DropTable(
                name: "tickets_sending",
                schema: "metal_link");
        }
    }
}
