using JournalApp.Application.Common.Interfaces;
using JournalApp.Domain.Exceptions;

namespace JournalApp.Application.Common;

public static class CurrentUserExtensions
{
    public static Guid GetRequiredUserId(this ICurrentUserService currentUser) =>
        currentUser.UserId ?? throw new UnauthorizedException("The request is not authenticated.");
}
