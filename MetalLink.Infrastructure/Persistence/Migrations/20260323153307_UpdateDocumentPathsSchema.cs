using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDocumentPathsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cipro_document_path",
                schema: "metal_link",
                table: "document_paths");

            migrationBuilder.DropColumn(
                name: "trading_license",
                schema: "metal_link",
                table: "document_paths");

            migrationBuilder.AlterColumn<string>(
                name: "cipc_document_path",
                schema: "metal_link",
                table: "document_paths",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bbbee_compliance_certificate_path",
                schema: "metal_link",
                table: "document_paths",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_clearance_certificate_path",
                schema: "metal_link",
                table: "document_paths",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trading_license_path",
                schema: "metal_link",
                table: "document_paths",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vat_registration_certificate_path",
                schema: "metal_link",
                table: "document_paths",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bbbee_compliance_certificate_path",
                schema: "metal_link",
                table: "document_paths");

            migrationBuilder.DropColumn(
                name: "tax_clearance_certificate_path",
                schema: "metal_link",
                table: "document_paths");

            migrationBuilder.DropColumn(
                name: "trading_license_path",
                schema: "metal_link",
                table: "document_paths");

            migrationBuilder.DropColumn(
                name: "vat_registration_certificate_path",
                schema: "metal_link",
                table: "document_paths");

            migrationBuilder.AlterColumn<string>(
                name: "cipc_document_path",
                schema: "metal_link",
                table: "document_paths",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cipro_document_path",
                schema: "metal_link",
                table: "document_paths",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trading_license",
                schema: "metal_link",
                table: "document_paths",
                type: "text",
                nullable: true);
        }
    }
}
