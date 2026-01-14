using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMovementsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    StockMovementId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SiteId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    TicketId = table.Column<long>(type: "bigint", nullable: false),
                    TicketLineId = table.Column<long>(type: "bigint", nullable: true),
                    MovementType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QuantityKg = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UnitPricePerKg = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "ZAR"),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CounterpartyName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CounterpartyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.StockMovementId);
                    table.ForeignKey(
                        name: "FK_StockMovements_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "SiteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "TicketId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_TicketLines_TicketLineId",
                        column: x => x.TicketLineId,
                        principalTable: "TicketLines",
                        principalColumn: "TicketLineId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SiteId",
                table: "StockMovements",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId",
                table: "StockMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TicketId",
                table: "StockMovements",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TicketLineId",
                table: "StockMovements",
                column: "TicketLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MovementType",
                table: "StockMovements",
                column: "MovementType");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CreatedTime",
                table: "StockMovements",
                column: "CreatedTime");

            // Create view for stock on hand by product and site
            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW ""StockOnHand"" AS
                SELECT 
                    sm.""SiteId"",
                    sm.""ProductId"",
                    p.""ProductName"",
                    s.""SiteName"",
                    SUM(sm.""QuantityKg"") as ""QuantityKg"",
                    COUNT(DISTINCT sm.""TicketId"") as ""TransactionCount"",
                    MAX(sm.""CreatedTime"") as ""LastMovementTime""
                FROM ""StockMovements"" sm
                INNER JOIN ""Products"" p ON sm.""ProductId"" = p.""ProductId""
                INNER JOIN ""Sites"" s ON sm.""SiteId"" = s.""SiteId""
                WHERE sm.""IsActive"" = true
                GROUP BY sm.""SiteId"", sm.""ProductId"", p.""ProductName"", s.""SiteName""
                HAVING SUM(sm.""QuantityKg"") > 0
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS ""StockOnHand""");
            
            migrationBuilder.DropTable(
                name: "StockMovements");
        }
    }
}
