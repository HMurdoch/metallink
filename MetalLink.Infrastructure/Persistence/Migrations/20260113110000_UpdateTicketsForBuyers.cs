using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTicketsForBuyers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make CustomerId nullable (receiving tickets have CustomerId, delivery tickets have BuyerId)
            migrationBuilder.AlterColumn<long>(
                name: "CustomerId",
                table: "Tickets",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            // Add BuyerId column for delivery/sending tickets
            migrationBuilder.AddColumn<long>(
                name: "BuyerId",
                table: "Tickets",
                type: "bigint",
                nullable: true);

            // Add foreign key to Buyers
            migrationBuilder.CreateIndex(
                name: "IX_Tickets_BuyerId",
                table: "Tickets",
                column: "BuyerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Buyers_BuyerId",
                table: "Tickets",
                column: "BuyerId",
                principalTable: "Buyers",
                principalColumn: "BuyerId",
                onDelete: ReferentialAction.Restrict);

            // Add check constraint: either CustomerId OR BuyerId must be set (not both, not neither)
            migrationBuilder.Sql(@"
                ALTER TABLE ""Tickets"" 
                ADD CONSTRAINT ""CK_Tickets_CustomerOrBuyer"" 
                CHECK (
                    (""CustomerId"" IS NOT NULL AND ""BuyerId"" IS NULL) OR 
                    (""CustomerId"" IS NULL AND ""BuyerId"" IS NOT NULL)
                )
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Tickets"" DROP CONSTRAINT IF EXISTS ""CK_Tickets_CustomerOrBuyer""");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Buyers_BuyerId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_BuyerId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "BuyerId",
                table: "Tickets");

            migrationBuilder.AlterColumn<long>(
                name: "CustomerId",
                table: "Tickets",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
