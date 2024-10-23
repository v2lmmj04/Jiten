using Jiten.Core;
using Jiten.Core.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jiten.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaDeckController(JitenDbContext context) : ControllerBase
{
    [HttpGet("GetAll")]
    public async Task<List<Deck>> GetAll()
    {
        return await context.Decks.AsNoTracking().ToListAsync();
    }
}