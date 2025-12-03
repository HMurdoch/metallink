using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tickets",
                schema: "metal_link",
                columns: table => new
                {
                    ticket_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<long>(type: "bigint", nullable: false),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    operator_id = table.Column<long>(type: "bigint", nullable: false),
                    ticket_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ticket_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    first_weight_kg = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    second_weight_kg = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    net_weight_kg = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    unit_price_per_kg = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    product_description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tickets_ticket_id", x => x.ticket_id);
                    table.ForeignKey(
                        name: "fk_tickets_customer_id_customers",
                        column: x => x.customer_id,
                        principalSchema: "metal_link",
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tickets_operator_id_operators",
                        column: x => x.operator_id,
                        principalSchema: "metal_link",
                        principalTable: "operators",
                        principalColumn: "operator_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tickets_operator_id",
                schema: "metal_link",
                table: "tickets",
                column: "operator_id");

            migrationBuilder.CreateIndex(
                name: "tickets_customer_id_idx",
                schema: "metal_link",
                table: "tickets",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "tickets_site_id_idx",
                schema: "metal_link",
                table: "tickets",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "tickets_ticket_number_idx",
                schema: "metal_link",
                table: "tickets",
                column: "ticket_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tickets",
                schema: "metal_link");
        }
    }
}
