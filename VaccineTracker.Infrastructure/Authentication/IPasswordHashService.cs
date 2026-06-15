namespace VaccineTracker.Infrastructure.Authentication;

public interface IPasswordHashService
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash);
}
