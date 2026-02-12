using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByOperatorNavigationToTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create foreign key constraints for CreatedByOperator in receiving_tickets
            migrationBuilder.CreateIndex(
                name: "IX_receiving_tickets_created_by_operator_id",
                table: "receiving_tickets",
                column: "created_by_operator_id");

            migrationBuilder.AddForeignKey(
                name: "FK_receiving_tickets_operators_created_by_operator_id",
                table: "receiving_tickets",
                column: "created_by_operator_id",
                principalTable: "operators",
                principalColumn: "operator_id",
                onDelete: ReferentialAction.Restrict);

            // Create foreign key constraints for CreatedByOperator in receiving_ticket_lines
            migrationBuilder.CreateIndex(
                name: "IX_receiving_ticket_lines_created_by_operator_id",
                table: "receiving_ticket_lines",
                column: "created_by_operator_id");

            migrationBuilder.AddForeignKey(
                name: "FK_receiving_ticket_lines_operators_created_by_operator_id",
                table: "receiving_ticket_lines",
                column: "created_by_operator_id",
                principalTable: "operators",
                principalColumn: "operator_id",
                onDelete: ReferentialAction.Restrict);

            // Create foreign key constraints for CreatedByOperator in sending_tickets
            migrationBuilder.CreateIndex(
                name: "IX_sending_tickets_created_by_operator_id",
                table: "sending_tickets",
                column: "created_by_operator_id");

            migrationBuilder.AddForeignKey(
                name: "FK_sending_tickets_operators_created_by_operator_id",
                table: "sending_tickets",
                column: "created_by_operator_id",
                principalTable: "operators",
                principalColumn: "operator_id",
                onDelete: ReferentialAction.Restrict);

            // Create foreign key constraints for CreatedByOperator in sending_ticket_lines
            migrationBuilder.CreateIndex(
                name: "IX_sending_ticket_lines_created_by_operator_id",
                table: "sending_ticket_lines",
                column: "created_by_operator_id");

            migrationBuilder.AddForeignKey(
                name: "FK_sending_ticket_lines_operators_created_by_operator_id",
                table: "sending_ticket_lines",
                column: "created_by_operator_id",
                principalTable: "operators",
                principalColumn: "operator_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign keys and indexes for receiving_tickets
            migrationBuilder.DropForeignKey(
                name: "FK_receiving_tickets_operators_created_by_operator_id",
                table: "receiving_tickets");

            migrationBuilder.DropIndex(
                name: "IX_receiving_tickets_created_by_operator_id",
                table: "receiving_tickets");

            // Drop foreign keys and indexes for receiving_ticket_lines
            migrationBuilder.DropForeignKey(
                name: "FK_receiving_ticket_lines_operators_created_by_operator_id",
                table: "receiving_ticket_lines");

            migrationBuilder.DropIndex(
                name: "IX_receiving_ticket_lines_created_by_operator_id",
                table: "receiving_ticket_lines");

            // Drop foreign keys and indexes for sending_tickets
            migrationBuilder.DropForeignKey(
                name: "FK_sending_tickets_operators_created_by_operator_id",
                table: "sending_tickets");

            migrationBuilder.DropIndex(
                name: "IX_sending_tickets_created_by_operator_id",
                table: "sending_tickets");

            // Drop foreign keys and indexes for sending_ticket_lines
            migrationBuilder.DropForeignKey(
                name: "FK_sending_ticket_lines_operators_created_by_operator_id",
                table: "sending_ticket_lines");

            migrationBuilder.DropIndex(
                name: "IX_sending_ticket_lines_created_by_operator_id",
                table: "sending_ticket_lines");
        }
    }
}
