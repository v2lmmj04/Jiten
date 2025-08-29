using System.Security.Claims;

namespace Jiten.Api.Services;

public interface ICurrentUserService
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
    ClaimsPrincipal? Principal { get; }
}
