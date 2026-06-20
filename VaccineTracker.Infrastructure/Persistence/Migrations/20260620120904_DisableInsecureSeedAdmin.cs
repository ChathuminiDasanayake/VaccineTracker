using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaccineTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DisableInsecureSeedAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            UPDATE [Users]
            SET
                [PasswordHash] = N'DISABLED_BOOTSTRAP_ACCOUNT',
                [Status] = 2,
                [UpdatedAt] = SYSUTCDATETIME(),
                [UpdatedBy] = N'SecurityMigration'
            WHERE [Id] = '11111111-1111-1111-1111-111111111111'
            """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
