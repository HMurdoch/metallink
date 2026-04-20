using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CleanupPriceCodeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "price_a",
                schema: "metal_link",
                table: "prices");

            migrationBuilder.DropColumn(
                name: "price_b",
                schema: "metal_link",
                table: "prices");

            migrationBuilder.DropColumn(
                name: "price_c",
                schema: "metal_link",
                table: "prices");

            migrationBuilder.DropColumn(
                name: "price_code",
                schema: "metal_link",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "price_code",
                schema: "metal_link",
                table: "buyers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "price_a",
                schema: "metal_link",
                table: "prices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "price_b",
                schema: "metal_link",
                table: "prices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "price_c",
                schema: "metal_link",
                table: "prices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "price_code",
                schema: "metal_link",
                table: "customers",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "price_code",
                schema: "metal_link",
                table: "buyers",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);
        }
    }
}
