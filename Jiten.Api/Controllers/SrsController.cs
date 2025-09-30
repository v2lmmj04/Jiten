using Jiten.Api.Services;
using Jiten.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jiten.Api.Controllers;

[ApiController]
[Route("api/srs")]
[Authorize]
public class SrsController(JitenDbContext context, ICurrentUserService currentUserService) : ControllerBase
{
    
}