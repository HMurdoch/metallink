using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ModelOnlyFixNavigations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Empty - model-only migration to fix EF Core shadow property issue
            // The database schema is already correct, only EF's model mapping was wrong
            
            // Original migration tried to drop shadow FKs that don't exist in the DB
            /*
            migrationBuilder.DropForeignKey(
                name: "FK_customer_documents_customers_CustomerId1",
                schema: "metal_link",
                table: "customer_documents");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_documents_customer_id_customers",
                schema: "metal_link",
                table: "customer_documents");

            migrationBuilder.DropForeignKey(
                name: "FK_tickets_currencies_CurrencyId1",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_tickets_customers_CustomerId1",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_tickets_operators_OperatorId1",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_tickets_products_ProductId1",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_tickets_sites_SiteId1",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_tickets_currency_id_currencies",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_tickets_customer_id_customers",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_tickets_operator_id_operators",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_tickets_product_id_products",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_tickets_site_id_sites",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_CurrencyId1",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_CustomerId1",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_OperatorId1",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_ProductId1",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_SiteId1",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_customer_documents_CustomerId1",
                schema: "metal_link",
                table: "customer_documents");

            migrationBuilder.DropColumn(
                name: "CurrencyId1",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "CustomerId1",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "OperatorId1",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "SiteId1",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "CustomerId1",
                schema: "metal_link",
                table: "customer_documents");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "metal_link",
                table: "customer_documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedTime",
                schema: "metal_link",
                table: "customer_documents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddForeignKey(
                name: "FK_customer_documents_customers_customer_id",
                schema: "metal_link",
                table: "customer_documents",
                column: "customer_id",
                principalSchema: "metal_link",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_currency_id_currencies",
                schema: "metal_link",
                table: "tickets",
                column: "currency_id",
                principalSchema: "metal_link",
                principalTable: "currencies",
                principalColumn: "currency_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_customer_id_customers",
                schema: "metal_link",
                table: "tickets",
                column: "customer_id",
                principalSchema: "metal_link",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_operator_id_operators",
                schema: "metal_link",
                table: "tickets",
                column: "operator_id",
                principalSchema: "metal_link",
                principalTable: "operators",
                principalColumn: "operator_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_product_id_products",
                schema: "metal_link",
                table: "tickets",
                column: "product_id",
                principalSchema: "metal_link",
                principalTable: "products",
                principalColumn: "product_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_site_id_sites",
                schema: "metal_link",
                table: "tickets",
                column: "site_id",
                principalSchema: "metal_link",
                principalTable: "sites",
                principalColumn: "site_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_customer_documents_customers_customer_id",
                schema: "metal_link",
                table: "customer_documents");

            migrationBuilder.DropForeignKey(
                name: "fk_tickets_currency_id_currencies",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_tickets_customer_id_customers",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_tickets_operator_id_operators",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_tickets_product_id_products",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_tickets_site_id_sites",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "metal_link",
                table: "customer_documents");

            migrationBuilder.DropColumn(
                name: "UpdatedTime",
                schema: "metal_link",
                table: "customer_documents");

            migrationBuilder.AddColumn<long>(
                name: "CurrencyId1",
                schema: "metal_link",
                table: "tickets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CustomerId1",
                schema: "metal_link",
                table: "tickets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "OperatorId1",
                schema: "metal_link",
                table: "tickets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ProductId1",
                schema: "metal_link",
                table: "tickets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SiteId1",
                schema: "metal_link",
                table: "tickets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "CustomerId1",
                schema: "metal_link",
                table: "customer_documents",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tickets_CurrencyId1",
                schema: "metal_link",
                table: "tickets",
                column: "CurrencyId1");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_CustomerId1",
                schema: "metal_link",
                table: "tickets",
                column: "CustomerId1");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_OperatorId1",
                schema: "metal_link",
                table: "tickets",
                column: "OperatorId1");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_ProductId1",
                schema: "metal_link",
                table: "tickets",
                column: "ProductId1");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_SiteId1",
                schema: "metal_link",
                table: "tickets",
                column: "SiteId1");

            migrationBuilder.CreateIndex(
                name: "IX_customer_documents_CustomerId1",
                schema: "metal_link",
                table: "customer_documents",
                column: "CustomerId1");

            migrationBuilder.AddForeignKey(
                name: "FK_customer_documents_customers_CustomerId1",
                schema: "metal_link",
                table: "customer_documents",
                column: "CustomerId1",
                principalSchema: "metal_link",
                principalTable: "customers",
                principalColumn: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_customer_documents_customer_id_customers",
                schema: "metal_link",
                table: "customer_documents",
                column: "customer_id",
                principalSchema: "metal_link",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_currencies_CurrencyId1",
                schema: "metal_link",
                table: "tickets",
                column: "CurrencyId1",
                principalSchema: "metal_link",
                principalTable: "currencies",
                principalColumn: "currency_id");

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_customers_CustomerId1",
                schema: "metal_link",
                table: "tickets",
                column: "CustomerId1",
                principalSchema: "metal_link",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_operators_OperatorId1",
                schema: "metal_link",
                table: "tickets",
                column: "OperatorId1",
                principalSchema: "metal_link",
                principalTable: "operators",
                principalColumn: "operator_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_products_ProductId1",
                schema: "metal_link",
                table: "tickets",
                column: "ProductId1",
                principalSchema: "metal_link",
                principalTable: "products",
                principalColumn: "product_id");

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_sites_SiteId1",
                schema: "metal_link",
                table: "tickets",
                column: "SiteId1",
                principalSchema: "metal_link",
                principalTable: "sites",
                principalColumn: "site_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_currency_id_currencies",
                schema: "metal_link",
                table: "tickets",
                column: "currency_id",
                principalSchema: "metal_link",
                principalTable: "currencies",
                principalColumn: "currency_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_customer_id_customers",
                schema: "metal_link",
                table: "tickets",
                column: "customer_id",
                principalSchema: "metal_link",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_operator_id_operators",
                schema: "metal_link",
                table: "tickets",
                column: "operator_id",
                principalSchema: "metal_link",
                principalTable: "operators",
                principalColumn: "operator_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_product_id_products",
                schema: "metal_link",
                table: "tickets",
                column: "product_id",
                principalSchema: "metal_link",
                principalTable: "products",
                principalColumn: "product_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_site_id_sites",
                schema: "metal_link",
                table: "tickets",
                column: "site_id",
                principalSchema: "metal_link",
                principalTable: "sites",
                principalColumn: "site_id",
                onDelete: ReferentialAction.Cascade);
            */
        }
    }
}
