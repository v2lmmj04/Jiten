using Jiten.Core;
using Jiten.Core.Data.JMDict;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jiten.Api.Controllers;

[ApiController]
[Route("api/vocabulary")]
public class VocabularyController(JitenDbContext context) : ControllerBase
{
    [HttpGet("{wordId}/{readingType}/{readingId}")]
    public async Task<IResult> GetWord(int wordId, JmDictReadingType readingType, int readingId)
    {
        var word = await context.JMDictWords.AsNoTracking()
                             .Include(w => w.Definitions)
                             .Include(w => w.Readings)
                             .Include(w => w.Definitions)
                             .FirstOrDefaultAsync(w => w.WordId == wordId);

        if (word == null)
            return Results.NotFound();
        

        string reading = readingType switch
                         {
                             JmDictReadingType.Reading => word.Readings[readingId],
                             JmDictReadingType.KanaReading => word.Readings[readingId],
                             _ => throw new ArgumentOutOfRangeException(nameof(readingType), readingType, null)
                         };

        return Results.Ok(new
                          {
                              word.WordId,
                              word.Readings,
                              word.Definitions,
                              reading
                          });
    }
}