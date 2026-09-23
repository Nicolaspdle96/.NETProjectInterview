namespace TaskManager.Application.Abstractions.Persistence;

/// <summary>
/// Raised by <see cref="IUnitOfWork"/> when a save violates a unique constraint, e.g. two
/// concurrent registrations with the same email that both passed the existence check.
/// </summary>
public sealed class UniqueConstraintException(string message, Exception innerException)
    : Exception(message, innerException);
