namespace WorkForceGovProject.Exceptions;

/// <summary>
/// Thrown when a requested resource cannot be found (maps to HTTP 404).
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.") { }
}
