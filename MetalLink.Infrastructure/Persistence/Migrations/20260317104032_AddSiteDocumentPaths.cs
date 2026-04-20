using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteDocumentPaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Note: Constraints were already missing in some environments, skipping drop.
            
            migrationBuilder.AlterColumn<int>(
                name: "province_id",
                schema: "metal_link",
                table: "sites",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "country_id",
                schema: "metal_link",
                table: "sites",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "document_path_id",
                schema: "metal_link",
                table: "sites",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "document_paths",
                schema: "metal_link",
                columns: table => new
                {
                    document_path_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cipc_document_path = table.Column<string>(type: "text", nullable: true),
                    trading_license = table.Column<string>(type: "text", nullable: true),
                    cipro_document_path = table.Column<string>(type: "text", nullable: true),
                    created_by_operator_id = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_paths", x => x.document_path_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sites_document_path_id",
                schema: "metal_link",
                table: "sites",
                column: "document_path_id");

            migrationBuilder.AddForeignKey(
                name: "FK_sites_countries_country_id",
                schema: "metal_link",
                table: "sites",
                column: "country_id",
                principalSchema: "metal_link",
                principalTable: "countries",
                principalColumn: "country_id");

            migrationBuilder.AddForeignKey(
                name: "FK_sites_document_paths_document_path_id",
                schema: "metal_link",
                table: "sites",
                column: "document_path_id",
                principalSchema: "metal_link",
                principalTable: "document_paths",
                principalColumn: "document_path_id");

            migrationBuilder.AddForeignKey(
                name: "FK_sites_provinces_province_id",
                schema: "metal_link",
                table: "sites",
                column: "province_id",
                principalSchema: "metal_link",
                principalTable: "provinces",
                principalColumn: "province_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropForeignKey(
            //    name: "FK_sites_countries_country_id",
            //    schema: "metal_link",
            //    table: "sites");

            migrationBuilder.DropForeignKey(
                name: "FK_sites_document_paths_document_path_id",
                schema: "metal_link",
                table: "sites");

            // migrationBuilder.DropForeignKey(
            //    name: "FK_sites_provinces_province_id",
            //    schema: "metal_link",
            //    table: "sites");

            migrationBuilder.DropTable(
                name: "document_paths",
                schema: "metal_link");

            migrationBuilder.DropIndex(
                name: "IX_sites_document_path_id",
                schema: "metal_link",
                table: "sites");

            migrationBuilder.DropColumn(
                name: "document_path_id",
                schema: "metal_link",
                table: "sites");

            migrationBuilder.AlterColumn<int>(
                name: "province_id",
                schema: "metal_link",
                table: "sites",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "country_id",
                schema: "metal_link",
                table: "sites",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_sites_countries_country_id",
                schema: "metal_link",
                table: "sites",
                column: "country_id",
                principalSchema: "metal_link",
                principalTable: "countries",
                principalColumn: "country_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_sites_provinces_province_id",
                schema: "metal_link",
                table: "sites",
                column: "province_id",
                principalSchema: "metal_link",
                principalTable: "provinces",
                principalColumn: "province_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
