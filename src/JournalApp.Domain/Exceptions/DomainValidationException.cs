namespace JournalApp.Domain.Exceptions;

/// <summary>
/// Thrown when input violates a business rule. Carries the errors grouped by field name.
/// </summary>
public class DomainValidationException : Exception
{
    public DomainValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public DomainValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = [error] })
    {
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
