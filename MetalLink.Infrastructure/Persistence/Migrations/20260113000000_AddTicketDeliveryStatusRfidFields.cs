using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketDeliveryStatusRfidFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryNumber",
                table: "Tickets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Tickets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "receiving");

            migrationBuilder.AddColumn<string>(
                name: "RfidCardNumber",
                table: "Tickets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryNumber",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "RfidCardNumber",
                table: "Tickets");
        }
    }
}
