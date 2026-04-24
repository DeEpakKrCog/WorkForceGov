namespace WorkForceGovProject.Exceptions;

/// <summary>
/// Thrown when a caller lacks permission for an operation (maps to HTTP 403).
/// </summary>
public sealed class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "You are not authorized to perform this action.")
        : base(message) { }
}
