using Jiten.Api.Dtos;
using Jiten.Core;
using Jiten.Core.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Z.EntityFramework.Plus;

namespace Jiten.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaDeckController(JitenDbContext context) : ControllerBase
{
    [HttpGet("GetAll")]
    public List<Deck> GetAll()
    {
        return context.Decks.AsNoTracking().ToList();
    }

    [HttpGet("{id}/vocabulary")]
    public List<WordDto> GetVocabulary(int id, string? sortOrder = "", int? offset = 0)
    {
        var deckWords = context.DeckWords.AsNoTracking()
                               .Where(dw => dw.DeckId == id)
                               .OrderBy(dw => dw.Id)
                               .Skip(offset ?? 0)
                               .Take(100)
                               .ToList();

        var wordIds = deckWords.Select(dw => dw.WordId).ToList();

        var jmdictWords = context.JMDictWords.AsNoTracking()
                                 .Where(w => wordIds.Contains(w.WordId))
                                 .Include(w => w.Definitions)
                                 .ToList();

        var words = deckWords.Select(dw => new { dw, jmDictWord = jmdictWords.FirstOrDefault(w => w.WordId == dw.WordId) })
                             .OrderBy(dw => wordIds.IndexOf(dw.dw.WordId))
                             .ToList();
        List<WordDto> dto = new();

        foreach (var word in words)
        {
            if (word.jmDictWord == null)
            {
                continue;
            }

            var reading = word.dw.ReadingType == 0
                ? word.jmDictWord.Readings[word.dw.ReadingIndex]
                : word.jmDictWord.KanaReadings[word.dw.ReadingIndex];
            var alternativeReadings = word.jmDictWord.Readings.Concat(word.jmDictWord.KanaReadings).ToList();
            alternativeReadings.RemoveAll(r => r == reading);

            int i = 1;
            List<DefinitionDto> definitions = new();
            foreach (var definition in word.jmDictWord.Definitions.OrderBy(d => d.DefinitionId))
            {
                if (definition.EnglishMeanings.Count == 0)
                    continue;
                
                definitions.Add(new DefinitionDto
                                {
                                    Index = i++, Meanings = definition.EnglishMeanings, PartsOfSpeech = definition.PartsOfSpeech
                                });
            }

            var wordDto = new WordDto
                          {
                              WordId = word.jmDictWord.WordId,
                              Reading = reading,
                              AlternativeReadings = alternativeReadings,
                              PartsOfSpeech = word.jmDictWord.PartsOfSpeech,
                              Definitions = definitions
                          };

            dto.Add(wordDto);
        }

        return dto;
    }
}