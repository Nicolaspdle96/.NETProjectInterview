namespace TaskManager.Domain.Common;

/// <summary>
/// Thrown when an entity invariant is violated. Expected input errors are caught
/// earlier by Application validators, so reaching this indicates a programming error.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
