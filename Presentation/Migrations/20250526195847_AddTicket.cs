using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Presentation.Migrations
{
    /// <inheritdoc />
    public partial class AddTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketEntity_Payments_PaymentId",
                table: "TicketEntity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TicketEntity",
                table: "TicketEntity");

            migrationBuilder.RenameTable(
                name: "TicketEntity",
                newName: "Tickets");

            migrationBuilder.RenameIndex(
                name: "IX_TicketEntity_PaymentId",
                table: "Tickets",
                newName: "IX_Tickets_PaymentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Payments_PaymentId",
                table: "Tickets",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "PaymentId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Payments_PaymentId",
                table: "Tickets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets");

            migrationBuilder.RenameTable(
                name: "Tickets",
                newName: "TicketEntity");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_PaymentId",
                table: "TicketEntity",
                newName: "IX_TicketEntity_PaymentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TicketEntity",
                table: "TicketEntity",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketEntity_Payments_PaymentId",
                table: "TicketEntity",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "PaymentId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
