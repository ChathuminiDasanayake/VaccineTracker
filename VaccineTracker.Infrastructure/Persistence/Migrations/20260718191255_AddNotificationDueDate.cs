using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaccineTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDueDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DueDate",
                table: "NotificationOutbox",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutbox_Status_DueDate",
                table: "NotificationOutbox",
                columns: new[] { "Status", "DueDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationOutbox_Status_DueDate",
                table: "NotificationOutbox");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "NotificationOutbox");
        }
    }
}
