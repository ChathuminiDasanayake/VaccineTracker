namespace VaccineTracker.Application.Interfaces;

public interface ICurrentUser
{
    Guid UserId { get; }

    Guid? HospitalId { get; }

    string Email { get; }

    string Role { get; }
}
