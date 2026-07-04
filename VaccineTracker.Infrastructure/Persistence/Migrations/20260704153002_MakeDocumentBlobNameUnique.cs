using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaccineTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeDocumentBlobNameUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Documents_BlobName",
                table: "Documents",
                column: "BlobName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_BlobName",
                table: "Documents");
        }
    }
}
