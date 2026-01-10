using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tickets_Refactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "total_amount",
                schema: "metal_link",
                table: "tickets",
                newName: "total_amount_ex_vat");


            migrationBuilder.AddColumn<string>(
                name: "ck_number",
                schema: "metal_link",
                table: "tickets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "currency_id",
                schema: "metal_link",
                table: "tickets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "driver_name",
                schema: "metal_link",
                table: "tickets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "foreign_ticket",
                schema: "metal_link",
                table: "tickets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "metal_link",
                table: "tickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ofm_weighbridge_ticket",
                schema: "metal_link",
                table: "tickets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "product_id",
                schema: "metal_link",
                table: "tickets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "total_incl_vat",
                schema: "metal_link",
                table: "tickets",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "trailer_registration",
                schema: "metal_link",
                table: "tickets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "vat_amount",
                schema: "metal_link",
                table: "tickets",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "vat_rate",
                schema: "metal_link",
                table: "tickets",
                type: "numeric(5,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "vehicle_registration",
                schema: "metal_link",
                table: "tickets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "product_code",
                schema: "metal_link",
                table: "products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "grade",
                schema: "metal_link",
                table: "products",
                type: "numeric(18,2)",
                nullable: true,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "price_c",
                schema: "metal_link",
                table: "prices",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "price_b",
                schema: "metal_link",
                table: "prices",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "price_a",
                schema: "metal_link",
                table: "prices",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.CreateTable(
                name: "currencies",
                schema: "metal_link",
                columns: table => new
                {
                    currency_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    currency_description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_currencies_currency_id", x => x.currency_id);
                });

            migrationBuilder.CreateTable(
                name: "ticket_lines",
                schema: "metal_link",
                columns: table => new
                {
                    ticket_line_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ticket_id = table.Column<long>(type: "bigint", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    weight_kg = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    unit_price_per_kg = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_incl_vat = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticket_lines_ticket_line_id", x => x.ticket_line_id);
                    table.ForeignKey(
                        name: "fk_ticket_lines_product_id_products",
                        column: x => x.product_id,
                        principalSchema: "metal_link",
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ticket_lines_ticket_id_tickets",
                        column: x => x.ticket_id,
                        principalSchema: "metal_link",
                        principalTable: "tickets",
                        principalColumn: "ticket_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tickets_currency_id",
                schema: "metal_link",
                table: "tickets",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_product_id",
                schema: "metal_link",
                table: "tickets",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "currencies_currency_code_idx",
                schema: "metal_link",
                table: "currencies",
                column: "currency_code");

            migrationBuilder.CreateIndex(
                name: "ticket_lines_product_id_idx",
                schema: "metal_link",
                table: "ticket_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ticket_lines_ticket_id_idx",
                schema: "metal_link",
                table: "ticket_lines",
                column: "ticket_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_currency_id_currencies",
                schema: "metal_link",
                table: "tickets",
                column: "currency_id",
                principalSchema: "metal_link",
                principalTable: "currencies",
                principalColumn: "currency_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_product_id_products",
                schema: "metal_link",
                table: "tickets",
                column: "product_id",
                principalSchema: "metal_link",
                principalTable: "products",
                principalColumn: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tickets_currency_id_currencies",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_tickets_product_id_products",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropTable(
                name: "currencies",
                schema: "metal_link");

            migrationBuilder.DropTable(
                name: "ticket_lines",
                schema: "metal_link");

            migrationBuilder.DropIndex(
                name: "IX_tickets_currency_id",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_product_id",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "ck_number",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "currency_id",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "driver_name",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "foreign_ticket",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "ofm_weighbridge_ticket",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "product_id",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "total_incl_vat",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "trailer_registration",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "vat_amount",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "vat_rate",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "vehicle_registration",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.RenameColumn(
                name: "total_amount_ex_vat",
                schema: "metal_link",
                table: "tickets",
                newName: "total_amount");

            migrationBuilder.AlterColumn<string>(
                name: "product_code",
                schema: "metal_link",
                table: "products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "grade",
                schema: "metal_link",
                table: "products",
                type: "numeric(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "price_c",
                schema: "metal_link",
                table: "prices",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "price_b",
                schema: "metal_link",
                table: "prices",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "price_a",
                schema: "metal_link",
                table: "prices",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");
        }
    }
}
