using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdCardImagePath",
                table: "Customers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverLicenseImagePath",
                table: "Customers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoImagePath",
                table: "Customers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureImagePath",
                table: "Customers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FingerprintImagePath",
                table: "Customers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdCardImagePath",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DriverLicenseImagePath",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PhotoImagePath",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "SignatureImagePath",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "FingerprintImagePath",
                table: "Customers");
        }
    }
}
