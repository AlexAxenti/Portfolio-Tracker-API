using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PortfolioTracker.Api.Auth;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            var userIdClaim =
                user?.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                user?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Authenticated user id is missing or invalid.");
            }

            return userId;
        }
    }
}
