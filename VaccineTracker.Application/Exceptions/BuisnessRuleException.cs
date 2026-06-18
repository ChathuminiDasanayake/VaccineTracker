namespace VaccineTracker.Application.Exceptions;

public sealed class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}