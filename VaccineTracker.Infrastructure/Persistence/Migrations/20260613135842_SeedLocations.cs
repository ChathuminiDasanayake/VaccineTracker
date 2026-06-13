using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaccineTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Locations",
                columns: new[]
                {
                    "Id",
                    "Street",
                    "City",
                    "State",
                    "PostalCode",
                    "Country",
                    "TenantId",
                    "CreatedAt",
                    "CreatedBy",
                    "UpdatedAt",
                    "UpdatedBy",
                    "IsDeleted"
                },
                values: new object[,]
                {
                    {
                        new Guid("22222222-2222-2222-2222-222222222221"),
                        "Main Street",
                        "Colombo",
                        "Western Province",
                        "00100",
                        "Sri Lanka",
                        null,
                        new DateTime(2026, 6, 13, 0, 0, 0, DateTimeKind.Utc),
                        "SeedLocations",
                        null,
                        null,
                        false
                    },
                    {
                        new Guid("22222222-2222-2222-2222-222222222222"),
                        "Hospital Road",
                        "Kandy",
                        "Central Province",
                        "20000",
                        "Sri Lanka",
                        null,
                        new DateTime(2026, 6, 13, 0, 0, 0, DateTimeKind.Utc),
                        "SeedLocations",
                        null,
                        null,
                        false
                    },
                    {
                        new Guid("22222222-2222-2222-2222-222222222223"),
                        "Galle Road",
                        "Galle",
                        "Southern Province",
                        "80000",
                        "Sri Lanka",
                        null,
                        new DateTime(2026, 6, 13, 0, 0, 0, DateTimeKind.Utc),
                        "SeedLocations",
                        null,
                        null,
                        false
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    new Guid("22222222-2222-2222-2222-222222222221"),
                    new Guid("22222222-2222-2222-2222-222222222222"),
                    new Guid("22222222-2222-2222-2222-222222222223")
                });
        }
    }
}
