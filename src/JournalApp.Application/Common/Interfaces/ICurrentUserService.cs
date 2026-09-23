namespace JournalApp.Application.Common.Interfaces;

public interface ICurrentUserService
{
    /// <summary>Id of the authenticated user, or <c>null</c> for anonymous requests.</summary>
    Guid? UserId { get; }
}
