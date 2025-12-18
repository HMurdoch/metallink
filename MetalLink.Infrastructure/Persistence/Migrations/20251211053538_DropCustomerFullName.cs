using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropCustomerFullName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op.
            // Columns full_name and address_* were already dropped manually from metal_link.customers.
            // This migration exists only so EF's migration history matches the current schema.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customers_company_id_companies",
                schema: "metal_link",
                table: "customers");

            migrationBuilder.DropForeignKey(
                name: "fk_customers_site_id_sites",
                schema: "metal_link",
                table: "customers");

            migrationBuilder.DropForeignKey(
                name: "fk_tickets_site_id_sites",
                schema: "metal_link",
                table: "tickets");

            migrationBuilder.DropTable(
                name: "sites",
                schema: "metal_link");

            migrationBuilder.DropTable(
                name: "companies",
                schema: "metal_link");

            migrationBuilder.DropTable(
                name: "provinces",
                schema: "metal_link");

            migrationBuilder.DropIndex(
                name: "IX_customers_company_id",
                schema: "metal_link",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_site_id",
                schema: "metal_link",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "company_id",
                schema: "metal_link",
                table: "customers");

            migrationBuilder.RenameColumn(
                name: "last_name",
                schema: "metal_link",
                table: "customers",
                newName: "suburb");

            migrationBuilder.RenameColumn(
                name: "first_name",
                schema: "metal_link",
                table: "customers",
                newName: "city");

            migrationBuilder.AlterColumn<long>(
                name: "site_id",
                schema: "metal_link",
                table: "customers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_line1",
                schema: "metal_link",
                table: "customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_line2",
                schema: "metal_link",
                table: "customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_name",
                schema: "metal_link",
                table: "customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "full_name",
                schema: "metal_link",
                table: "customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                schema: "metal_link",
                table: "customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
