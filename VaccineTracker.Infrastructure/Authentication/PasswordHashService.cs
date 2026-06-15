using System.Security.Cryptography;

namespace VaccineTracker.Infrastructure.Authentication;

public sealed class PasswordHashService : IPasswordHashService
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private const char SegmentDelimiter = '.';
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return string.Join(
            SegmentDelimiter,
            "pbkdf2-sha256",
            Iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var segments = passwordHash.Split(SegmentDelimiter);
        if (segments.Length != 4 ||
            segments[0] != "pbkdf2-sha256" ||
            !int.TryParse(segments[1], out var iterations))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(segments[2]);
            var expectedHash = Convert.FromBase64String(segments[3]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
