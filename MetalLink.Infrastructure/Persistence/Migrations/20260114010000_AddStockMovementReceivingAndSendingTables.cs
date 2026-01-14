using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMovementReceivingAndSendingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create stock_movements_receiving table
            migrationBuilder.CreateTable(
                name: "stock_movements_receiving",
                schema: "metal_link",
                columns: table => new
                {
                    stock_movement_receiving_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<long>(type: "bigint", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    ticket_receiving_id = table.Column<long>(type: "bigint", nullable: false),
                    ticket_receiving_line_id = table.Column<long>(type: "bigint", nullable: true),
                    quantity_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    unit_price_per_kg = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "ZAR"),
                    ticket_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    customer_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    movement_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movements_receiving", x => x.stock_movement_receiving_id);
                    table.ForeignKey(
                        name: "fk_stock_movements_receiving_sites_site_id",
                        column: x => x.site_id,
                        principalSchema: "metal_link",
                        principalTable: "sites",
                        principalColumn: "site_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_receiving_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "metal_link",
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_receiving_tickets_receiving_ticket_receiving_id",
                        column: x => x.ticket_receiving_id,
                        principalSchema: "metal_link",
                        principalTable: "tickets_receiving",
                        principalColumn: "ticket_receiving_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_receiving_ticket_receiving_lines_ticket_receiving_line_id",
                        column: x => x.ticket_receiving_line_id,
                        principalSchema: "metal_link",
                        principalTable: "ticket_receiving_lines",
                        principalColumn: "ticket_receiving_line_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_receiving_site_id",
                schema: "metal_link",
                table: "stock_movements_receiving",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_receiving_product_id",
                schema: "metal_link",
                table: "stock_movements_receiving",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_receiving_ticket_receiving_id",
                schema: "metal_link",
                table: "stock_movements_receiving",
                column: "ticket_receiving_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_receiving_ticket_receiving_line_id",
                schema: "metal_link",
                table: "stock_movements_receiving",
                column: "ticket_receiving_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_receiving_movement_date",
                schema: "metal_link",
                table: "stock_movements_receiving",
                column: "movement_date");

            // Create stock_movements_sending table
            migrationBuilder.CreateTable(
                name: "stock_movements_sending",
                schema: "metal_link",
                columns: table => new
                {
                    stock_movement_sending_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<long>(type: "bigint", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    ticket_sending_id = table.Column<long>(type: "bigint", nullable: false),
                    ticket_sending_line_id = table.Column<long>(type: "bigint", nullable: true),
                    quantity_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    unit_price_per_kg = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "ZAR"),
                    ticket_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    buyer_id = table.Column<long>(type: "bigint", nullable: false),
                    buyer_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    movement_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movements_sending", x => x.stock_movement_sending_id);
                    table.ForeignKey(
                        name: "fk_stock_movements_sending_sites_site_id",
                        column: x => x.site_id,
                        principalSchema: "metal_link",
                        principalTable: "sites",
                        principalColumn: "site_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_sending_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "metal_link",
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_sending_tickets_sending_ticket_sending_id",
                        column: x => x.ticket_sending_id,
                        principalSchema: "metal_link",
                        principalTable: "tickets_sending",
                        principalColumn: "ticket_sending_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_sending_ticket_sending_lines_ticket_sending_line_id",
                        column: x => x.ticket_sending_line_id,
                        principalSchema: "metal_link",
                        principalTable: "ticket_sending_lines",
                        principalColumn: "ticket_sending_line_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_sending_site_id",
                schema: "metal_link",
                table: "stock_movements_sending",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_sending_product_id",
                schema: "metal_link",
                table: "stock_movements_sending",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_sending_ticket_sending_id",
                schema: "metal_link",
                table: "stock_movements_sending",
                column: "ticket_sending_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_sending_ticket_sending_line_id",
                schema: "metal_link",
                table: "stock_movements_sending",
                column: "ticket_sending_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_sending_movement_date",
                schema: "metal_link",
                table: "stock_movements_sending",
                column: "movement_date");

            // Create stock_on_hand table
            migrationBuilder.CreateTable(
                name: "stock_on_hand",
                schema: "metal_link",
                columns: table => new
                {
                    stock_on_hand_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<long>(type: "bigint", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity_on_hand_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0),
                    total_received_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0),
                    total_sent_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0),
                    average_unit_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0),
                    total_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0),
                    last_movement_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_movement_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_on_hand", x => x.stock_on_hand_id);
                    table.ForeignKey(
                        name: "fk_stock_on_hand_sites_site_id",
                        column: x => x.site_id,
                        principalSchema: "metal_link",
                        principalTable: "sites",
                        principalColumn: "site_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_on_hand_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "metal_link",
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stock_on_hand_site_id",
                schema: "metal_link",
                table: "stock_on_hand",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_on_hand_product_id",
                schema: "metal_link",
                table: "stock_on_hand",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_on_hand_site_id_product_id",
                schema: "metal_link",
                table: "stock_on_hand",
                columns: new[] { "site_id", "product_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_movements_receiving",
                schema: "metal_link");

            migrationBuilder.DropTable(
                name: "stock_movements_sending",
                schema: "metal_link");

            migrationBuilder.DropTable(
                name: "stock_on_hand",
                schema: "metal_link");
        }
    }
}
