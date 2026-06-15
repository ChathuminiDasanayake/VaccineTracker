using System;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VaccineTracker.Infrastructure.Persistence;

#nullable disable

namespace VaccineTracker.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(VaccineTrackerDbContext))]
    [Migration("20260614120000_UpdateSeedAdminPassword")]
    public partial class UpdateSeedAdminPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var passwordHash = HashPassword("Ad@123").Replace("'", "''");

            migrationBuilder.Sql($"""
                UPDATE [Users]
                SET [PasswordHash] = N'{passwordHash}'
                WHERE [Id] = '11111111-1111-1111-1111-111111111111'
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [Users]
                SET [PasswordHash] = N'CHANGE_ME_HASHED_PASSWORD'
                WHERE [Id] = '11111111-1111-1111-1111-111111111111'
                """);
        }

        private static string HashPassword(string password)
        {
            const int saltSize = 16;
            const int keySize = 32;
            const int iterations = 100_000;

            var salt = RandomNumberGenerator.GetBytes(saltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                keySize);

            return string.Join(
                '.',
                "pbkdf2-sha256",
                iterations,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash));
        }
    }
}
