using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FinalSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "metal_link");

            migrationBuilder.CreateTable(
                name: "companies",
                schema: "metal_link",
                columns: table => new
                {
                    company_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    vat_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_companies_company_id", x => x.company_id);
                });

            migrationBuilder.CreateTable(
                name: "countries",
                schema: "metal_link",
                columns: table => new
                {
                    country_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_countries_country_id", x => x.country_id);
                });

            migrationBuilder.CreateTable(
                name: "operators",
                schema: "metal_link",
                columns: table => new
                {
                    operator_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<long>(type: "bigint", nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operators_operator_id", x => x.operator_id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "metal_link",
                columns: table => new
                {
                    product_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    grade = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products_product_id", x => x.product_id);
                });

            migrationBuilder.CreateTable(
                name: "provinces",
                schema: "metal_link",
                columns: table => new
                {
                    province_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_provinces_province_id", x => x.province_id);
                });

            migrationBuilder.CreateTable(
                name: "prices",
                schema: "metal_link",
                columns: table => new
                {
                    price_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    price_a = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price_b = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price_c = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prices_price_id", x => x.price_id);
                    table.ForeignKey(
                        name: "fk_prices_product_id_products",
                        column: x => x.product_id,
                        principalSchema: "metal_link",
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sites",
                schema: "metal_link",
                columns: table => new
                {
                    site_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    province_id = table.Column<int>(type: "integer", nullable: true),
                    country_id = table.Column<int>(type: "integer", nullable: true),
                    site_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    site_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    address_line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address_line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    suburb = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sites_site_id", x => x.site_id);
                    table.ForeignKey(
                        name: "fk_sites_company_id_companies",
                        column: x => x.company_id,
                        principalSchema: "metal_link",
                        principalTable: "companies",
                        principalColumn: "company_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sites_country_id_countries",
                        column: x => x.country_id,
                        principalSchema: "metal_link",
                        principalTable: "countries",
                        principalColumn: "country_id");
                    table.ForeignKey(
                        name: "fk_sites_province_id_provinces",
                        column: x => x.province_id,
                        principalSchema: "metal_link",
                        principalTable: "provinces",
                        principalColumn: "province_id");
                });

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "metal_link",
                columns: table => new
                {
                    customer_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: true),
                    site_id = table.Column<long>(type: "bigint", nullable: true),
                    first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_company = table.Column<bool>(type: "boolean", nullable: false),
                    id_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    account_number = table.Column<long>(type: "bigint", nullable: true)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    price_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    mobile_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    taxable = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.customer_id);
                    table.ForeignKey(
                        name: "FK_customers_sites_site_id",
                        column: x => x.site_id,
                        principalSchema: "metal_link",
                        principalTable: "sites",
                        principalColumn: "site_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_customers_company_id_companies",
                        column: x => x.company_id,
                        principalSchema: "metal_link",
                        principalTable: "companies",
                        principalColumn: "company_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_documents",
                schema: "metal_link",
                columns: table => new
                {
                    customer_document_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    document_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_documents_customer_document_id", x => x.customer_document_id);
                    table.ForeignKey(
                        name: "fk_customer_documents_customer_id_customers",
                        column: x => x.customer_id,
                        principalSchema: "metal_link",
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    table.ForeignKey(
                        name: "fk_tickets_site_id_sites",
                        column: x => x.site_id,
                        principalSchema: "metal_link",
                        principalTable: "sites",
                        principalColumn: "site_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "companies_company_name_idx",
                schema: "metal_link",
                table: "companies",
                column: "company_name");

            migrationBuilder.CreateIndex(
                name: "companies_vat_number_idx",
                schema: "metal_link",
                table: "companies",
                column: "vat_number");

            migrationBuilder.CreateIndex(
                name: "countries_code_idx",
                schema: "metal_link",
                table: "countries",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "countries_name_idx",
                schema: "metal_link",
                table: "countries",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "customer_documents_customer_id_idx",
                schema: "metal_link",
                table: "customer_documents",
                column: "customer_id");


            migrationBuilder.CreateIndex(
                name: "IX_customers_company_id",
                schema: "metal_link",
                table: "customers",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_customers_site_id",
                schema: "metal_link",
                table: "customers",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "operators_username_idx",
                schema: "metal_link",
                table: "operators",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "prices_product_id_idx",
                schema: "metal_link",
                table: "prices",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "products_product_code_idx",
                schema: "metal_link",
                table: "products",
                column: "product_code");

            migrationBuilder.CreateIndex(
                name: "products_product_name_idx",
                schema: "metal_link",
                table: "products",
                column: "product_name");

            migrationBuilder.CreateIndex(
                name: "provinces_code_idx",
                schema: "metal_link",
                table: "provinces",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "provinces_name_idx",
                schema: "metal_link",
                table: "provinces",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_sites_country_id",
                schema: "metal_link",
                table: "sites",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "IX_sites_province_id",
                schema: "metal_link",
                table: "sites",
                column: "province_id");

            migrationBuilder.CreateIndex(
                name: "sites_company_id_site_name_idx",
                schema: "metal_link",
                table: "sites",
                columns: new[] { "company_id", "site_name" });

            migrationBuilder.CreateIndex(
                name: "sites_site_code_idx",
                schema: "metal_link",
                table: "sites",
                column: "site_code");

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
                name: "customer_documents",
                schema: "metal_link");

            migrationBuilder.DropTable(
                name: "prices",
                schema: "metal_link");

            migrationBuilder.DropTable(
                name: "tickets",
                schema: "metal_link");

            migrationBuilder.DropTable(
                name: "products",
                schema: "metal_link");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "metal_link");

            migrationBuilder.DropTable(
                name: "operators",
                schema: "metal_link");

            migrationBuilder.DropTable(
                name: "sites",
                schema: "metal_link");

            migrationBuilder.DropTable(
                name: "companies",
                schema: "metal_link");

            migrationBuilder.DropTable(
                name: "countries",
                schema: "metal_link");

            migrationBuilder.DropTable(
                name: "provinces",
                schema: "metal_link");
        }
    }
}
