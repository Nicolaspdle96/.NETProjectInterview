namespace TaskManager.Application.Abstractions.Authentication;

public interface ICurrentUserService
{
    /// <summary>The id from the JWT <c>sub</c> claim, or <c>null</c> when the request is not authenticated.</summary>
    Guid? UserId { get; }
}
