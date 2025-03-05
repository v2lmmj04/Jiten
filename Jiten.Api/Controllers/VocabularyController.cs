using Jiten.Api.Dtos;
using Jiten.Api.Helpers;
using Jiten.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jiten.Api.Controllers;

[ApiController]
[Route("api/vocabulary")]
[EnableRateLimiting("fixed")]
public class VocabularyController(JitenDbContext context) : ControllerBase
{
    [HttpGet("{wordId}/{readingIndex}")]
    [ResponseCache(Duration = 3600)]
    public async Task<IResult> GetWord(int wordId, int readingIndex)
    {
        var word = await context.JMDictWords.AsNoTracking()
                                .Include(w => w.Definitions)
                                .FirstOrDefaultAsync(w => w.WordId == wordId);

        if (word == null)
            return Results.NotFound();

        var frequency = context.JmDictWordFrequencies.AsNoTracking().First(f => f.WordId == word.WordId);

        var mainReading = new ReadingDto()
                          {
                              Text = word.Readings[readingIndex],
                              ReadingIndex = readingIndex,
                              ReadingType = word.ReadingTypes[readingIndex],
                              FrequencyRank = frequency.ReadingsFrequencyRank[readingIndex],
                              FrequencyPercentage = frequency.ReadingsFrequencyPercentage[readingIndex],
                              UsedInMediaAmount = frequency.ReadingsUsedInMediaAmount[readingIndex]
                          };

        List<ReadingDto> alternativeReadings = word.Readings
                                                   .Select((r, i) => new ReadingDto
                                                                     {
                                                                         Text = r,
                                                                         ReadingIndex = i,
                                                                         ReadingType = word.ReadingTypes[i],
                                                                         FrequencyRank =
                                                                             frequency.ReadingsFrequencyRank[i],
                                                                         FrequencyPercentage =
                                                                             frequency.ReadingsFrequencyPercentage[i],
                                                                         UsedInMediaAmount = frequency.ReadingsUsedInMediaAmount[i]
                                                                     })
                                                   .ToList();

        return Results.Ok(new WordDto
                          {
                              WordId = word.WordId,
                              MainReading = mainReading,
                              AlternativeReadings = alternativeReadings,
                              Definitions = word.Definitions.ToDefinitionDtos(),
                              PartsOfSpeech = word.PartsOfSpeech,
                          });
    }
}