namespace TaskManager.Application.Common.Results;

public enum ErrorType
{
    Validation,
    Unauthorized,
    NotFound,
    Conflict,
}

/// <summary>An expected failure of a use case. The Api maps <see cref="Type"/> to an HTTP status.</summary>
public sealed record Error(
    string Code,
    string Description,
    ErrorType Type,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null)
{
    public static Error Validation(IReadOnlyDictionary<string, string[]> errors) =>
        new("Validation", "One or more validation errors occurred.", ErrorType.Validation, errors);

    public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
}
