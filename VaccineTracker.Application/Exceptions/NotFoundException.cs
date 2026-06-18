public sealed class NotFoundException : Exception
{
    public NotFoundException(string resource, object key)
        : base($"{resource} with identifier '{key}' was not found.")
    {
    }
}