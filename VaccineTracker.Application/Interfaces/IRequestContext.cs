namespace VaccineTracker.Application.Interfaces;

public interface IRequestContext
{
    string CorrelationId { get; }

    string? IpAddress { get; }

    string? UserAgent { get; }
}