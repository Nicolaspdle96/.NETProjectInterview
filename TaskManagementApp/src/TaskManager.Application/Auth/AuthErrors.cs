using TaskManager.Application.Common.Results;

namespace TaskManager.Application.Auth;

public static class AuthErrors
{
    public static readonly Error EmailAlreadyRegistered =
        Error.Conflict("Auth.EmailAlreadyRegistered", "An account with this email already exists.");

    /// <summary>Deliberately identical for unknown emails and wrong passwords.</summary>
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password.");

    public static readonly Error NotAuthenticated =
        Error.Unauthorized("Auth.NotAuthenticated", "The request is not authenticated.");
}
