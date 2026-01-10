using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangePricesAndGradientToTwoDecimals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Change Grade column from numeric(18,4) to numeric(18,2)
            migrationBuilder.AlterColumn<decimal>(
                name: "grade",
                schema: "metal_link",
                table: "products",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldNullable: true);

            // Change PriceA column from numeric(18,4) to numeric(18,2)
            migrationBuilder.AlterColumn<decimal>(
                name: "price_a",
                schema: "metal_link",
                table: "prices",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            // Change PriceB column from numeric(18,4) to numeric(18,2)
            migrationBuilder.AlterColumn<decimal>(
                name: "price_b",
                schema: "metal_link",
                table: "prices",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            // Change PriceC column from numeric(18,4) to numeric(18,2)
            migrationBuilder.AlterColumn<decimal>(
                name: "price_c",
                schema: "metal_link",
                table: "prices",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert Grade column back to numeric(18,4)
            migrationBuilder.AlterColumn<decimal>(
                name: "grade",
                schema: "metal_link",
                table: "products",
                type: "numeric(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true);

            // Revert PriceA column back to numeric(18,4)
            migrationBuilder.AlterColumn<decimal>(
                name: "price_a",
                schema: "metal_link",
                table: "prices",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            // Revert PriceB column back to numeric(18,4)
            migrationBuilder.AlterColumn<decimal>(
                name: "price_b",
                schema: "metal_link",
                table: "prices",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            // Revert PriceC column back to numeric(18,4)
            migrationBuilder.AlterColumn<decimal>(
                name: "price_c",
                schema: "metal_link",
                table: "prices",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");
        }
    }
}
