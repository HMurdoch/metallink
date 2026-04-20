using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PriceListRefactor_Phase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "must_declare",
                schema: "metal_link",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "product_price_list_id",
                schema: "metal_link",
                table: "customers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "product_price_list_id",
                schema: "metal_link",
                table: "buyers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "product_price_lists",
                schema: "metal_link",
                columns: table => new
                {
                    product_price_list_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_price_list_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    product_price_list_description = table.Column<string>(type: "text", nullable: true),
                    entity_flag = table.Column<char>(type: "character(1)", maxLength: 1, nullable: false),
                    created_by_operator_id = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_price_lists", x => x.product_price_list_id);
                    table.ForeignKey(
                        name: "FK_product_price_lists_operators_created_by_operator_id",
                        column: x => x.created_by_operator_id,
                        principalSchema: "metal_link",
                        principalTable: "operators",
                        principalColumn: "operator_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_price_list_product_prices",
                schema: "metal_link",
                columns: table => new
                {
                    product_price_list_product_price_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_price_list_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    created_by_operator_id = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_price_list_product_prices", x => x.product_price_list_product_price_id);
                    table.ForeignKey(
                        name: "FK_product_price_list_product_prices_operators_created_by_oper~",
                        column: x => x.created_by_operator_id,
                        principalSchema: "metal_link",
                        principalTable: "operators",
                        principalColumn: "operator_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_price_list_product_prices_product_price_lists_produ~",
                        column: x => x.product_price_list_id,
                        principalSchema: "metal_link",
                        principalTable: "product_price_lists",
                        principalColumn: "product_price_list_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_price_list_product_prices_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "metal_link",
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customers_product_price_list_id",
                schema: "metal_link",
                table: "customers",
                column: "product_price_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_buyers_product_price_list_id",
                schema: "metal_link",
                table: "buyers",
                column: "product_price_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_price_list_product_prices_created_by_operator_id",
                schema: "metal_link",
                table: "product_price_list_product_prices",
                column: "created_by_operator_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_price_list_product_prices_product_id",
                schema: "metal_link",
                table: "product_price_list_product_prices",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_price_list_product_prices_product_price_list_id_pro~",
                schema: "metal_link",
                table: "product_price_list_product_prices",
                columns: new[] { "product_price_list_id", "product_id", "is_active" },
                unique: true,
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "IX_product_price_lists_created_by_operator_id",
                schema: "metal_link",
                table: "product_price_lists",
                column: "created_by_operator_id");

            migrationBuilder.AddForeignKey(
                name: "FK_buyers_product_price_lists_product_price_list_id",
                schema: "metal_link",
                table: "buyers",
                column: "product_price_list_id",
                principalSchema: "metal_link",
                principalTable: "product_price_lists",
                principalColumn: "product_price_list_id");

            migrationBuilder.AddForeignKey(
                name: "FK_customers_product_price_lists_product_price_list_id",
                schema: "metal_link",
                table: "customers",
                column: "product_price_list_id",
                principalSchema: "metal_link",
                principalTable: "product_price_lists",
                principalColumn: "product_price_list_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_buyers_product_price_lists_product_price_list_id",
                schema: "metal_link",
                table: "buyers");

            migrationBuilder.DropForeignKey(
                name: "FK_customers_product_price_lists_product_price_list_id",
                schema: "metal_link",
                table: "customers");

            migrationBuilder.DropTable(
                name: "product_price_list_product_prices",
                schema: "metal_link");

            migrationBuilder.DropTable(
                name: "product_price_lists",
                schema: "metal_link");

            migrationBuilder.DropIndex(
                name: "IX_customers_product_price_list_id",
                schema: "metal_link",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_buyers_product_price_list_id",
                schema: "metal_link",
                table: "buyers");

            migrationBuilder.DropColumn(
                name: "must_declare",
                schema: "metal_link",
                table: "products");

            migrationBuilder.DropColumn(
                name: "product_price_list_id",
                schema: "metal_link",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "product_price_list_id",
                schema: "metal_link",
                table: "buyers");
        }
    }
}
