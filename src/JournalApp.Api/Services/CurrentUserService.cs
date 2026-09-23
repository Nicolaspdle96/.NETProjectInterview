using System.IdentityModel.Tokens.Jwt;
using JournalApp.Application.Common.Interfaces;

namespace JournalApp.Api.Services;

/// <summary>
/// Reads the authenticated user's id from the JWT <c>sub</c> claim of the current request.
/// </summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var sub = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }
}
