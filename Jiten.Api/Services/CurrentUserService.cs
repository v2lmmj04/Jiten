using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Jiten.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public string? UserId
    {
        get
        {
            var user = Principal;
            if (user == null)
                return null;

            // Prefer NameIdentifier claim (used by Identity), fallback to sub for JWT subject
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId)) return userId;
            return user.FindFirst("sub")?.Value;
        }
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
}
