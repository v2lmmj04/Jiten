using Jiten.Api.Dtos;
using Jiten.Core;
using Jiten.Core.Data;
using Jiten.Core.Data.JMDict;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Z.EntityFramework.Plus;

namespace Jiten.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaDeckController(JitenDbContext context) : ControllerBase
{
    [HttpGet("GetMediaDecks")]
    public PaginatedResponse<List<Deck>> GetMediaDecks(string? sortOrder = "", int? offset = 0, MediaType? mediaType = null)
    {
        int pageSize = 50;

        var query = context.Decks.AsNoTracking();

        if (mediaType != null)
            query = query.Where(d => d.MediaType == mediaType);

        var totalCount = query.Count();

        var decks = query.OrderBy(d => d.Id)
                         .Skip(offset ?? 0)
                         .Take(pageSize)
                         .ToList();

        return new PaginatedResponse<List<Deck>>(decks, totalCount, pageSize, offset ?? 0);
    }

    [HttpGet("{id}/vocabulary")]
    public PaginatedResponse<List<WordDto>> GetVocabulary(int id, string? sortOrder = "", int? offset = 0)
    {
        int pageSize = 100;

        int totalCount = context.DeckWords.AsNoTracking().Count(dw => dw.DeckId == id);

        var deckWords = context.DeckWords.AsNoTracking()
                               .Where(dw => dw.DeckId == id)
                               .OrderBy(dw => dw.Id)
                               .Skip(offset ?? 0)
                               .Take(pageSize)
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
                                    Index = i++,
                                    Meanings = definition.EnglishMeanings,
                                    PartsOfSpeech = definition.PartsOfSpeech.ToHumanReadablePartsOfSpeech()
                                });
            }

            var wordDto = new WordDto
                          {
                              WordId = word.jmDictWord.WordId,
                              Reading = reading,
                              AlternativeReadings = alternativeReadings,
                              PartsOfSpeech = word.jmDictWord.PartsOfSpeech.ToHumanReadablePartsOfSpeech(),
                              Definitions = definitions
                          };

            dto.Add(wordDto);
        }

        return new PaginatedResponse<List<WordDto>>(dto, totalCount, pageSize, offset ?? 0);
    }
}