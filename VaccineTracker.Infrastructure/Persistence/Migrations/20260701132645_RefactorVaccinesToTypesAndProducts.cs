using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaccineTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorVaccinesToTypesAndProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VaccinationRecords_Vaccines_VaccineId",
                table: "VaccinationRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_VaccineScheduleItems_Vaccines_VaccineId",
                table: "VaccineScheduleItems");

            migrationBuilder.DropTable(
                name: "Vaccines");

            migrationBuilder.RenameColumn(
                name: "VaccineId",
                table: "VaccineScheduleItems",
                newName: "VaccineTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_VaccineScheduleItems_VaccineId_TargetGroup_DoseNumber",
                table: "VaccineScheduleItems",
                newName: "IX_VaccineScheduleItems_VaccineTypeId_TargetGroup_DoseNumber");

            migrationBuilder.RenameColumn(
                name: "VaccineId",
                table: "VaccinationRecords",
                newName: "VaccineProductId");

            migrationBuilder.RenameIndex(
                name: "IX_VaccinationRecords_VaccineId",
                table: "VaccinationRecords",
                newName: "IX_VaccinationRecords_VaccineProductId");

            migrationBuilder.CreateTable(
                name: "VaccineTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DiseaseTarget = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccineTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VaccineProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VaccineTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManufacturerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccineProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaccineProducts_VaccineManufacturers_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "VaccineManufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VaccineProducts_VaccineTypes_VaccineTypeId",
                        column: x => x.VaccineTypeId,
                        principalTable: "VaccineTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VaccineProducts_Code",
                table: "VaccineProducts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaccineProducts_ManufacturerId",
                table: "VaccineProducts",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccineProducts_VaccineTypeId",
                table: "VaccineProducts",
                column: "VaccineTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccineTypes_Code",
                table: "VaccineTypes",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VaccinationRecords_VaccineProducts_VaccineProductId",
                table: "VaccinationRecords",
                column: "VaccineProductId",
                principalTable: "VaccineProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VaccineScheduleItems_VaccineTypes_VaccineTypeId",
                table: "VaccineScheduleItems",
                column: "VaccineTypeId",
                principalTable: "VaccineTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VaccinationRecords_VaccineProducts_VaccineProductId",
                table: "VaccinationRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_VaccineScheduleItems_VaccineTypes_VaccineTypeId",
                table: "VaccineScheduleItems");

            migrationBuilder.DropTable(
                name: "VaccineProducts");

            migrationBuilder.DropTable(
                name: "VaccineTypes");

            migrationBuilder.RenameColumn(
                name: "VaccineTypeId",
                table: "VaccineScheduleItems",
                newName: "VaccineId");

            migrationBuilder.RenameIndex(
                name: "IX_VaccineScheduleItems_VaccineTypeId_TargetGroup_DoseNumber",
                table: "VaccineScheduleItems",
                newName: "IX_VaccineScheduleItems_VaccineId_TargetGroup_DoseNumber");

            migrationBuilder.RenameColumn(
                name: "VaccineProductId",
                table: "VaccinationRecords",
                newName: "VaccineId");

            migrationBuilder.RenameIndex(
                name: "IX_VaccinationRecords_VaccineProductId",
                table: "VaccinationRecords",
                newName: "IX_VaccinationRecords_VaccineId");

            migrationBuilder.CreateTable(
                name: "Vaccines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManufacturerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DiseaseTarget = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vaccines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vaccines_VaccineManufacturers_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "VaccineManufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vaccines_Code",
                table: "Vaccines",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vaccines_ManufacturerId",
                table: "Vaccines",
                column: "ManufacturerId");

            migrationBuilder.AddForeignKey(
                name: "FK_VaccinationRecords_Vaccines_VaccineId",
                table: "VaccinationRecords",
                column: "VaccineId",
                principalTable: "Vaccines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VaccineScheduleItems_Vaccines_VaccineId",
                table: "VaccineScheduleItems",
                column: "VaccineId",
                principalTable: "Vaccines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
