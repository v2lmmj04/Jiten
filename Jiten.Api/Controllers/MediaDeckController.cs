using Jiten.Api.Dtos;
using Jiten.Core;
using Jiten.Core.Data;
using Jiten.Core.Data.JMDict;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jiten.Api.Controllers;

[ApiController]
[Route("api/media-deck")]
public class MediaDeckController(JitenDbContext context) : ControllerBase
{
    [HttpGet("get-media-decks")]
    public PaginatedResponse<List<Deck>> GetMediaDecks(string? sortOrder = "", int? offset = 0, MediaType? mediaType = null)
    {
        int pageSize = 50;

        var query = context.Decks.AsNoTracking();

        query = query.Where(d => d.ParentDeck == null);
        if (mediaType != null)
            query = query.Where(d => d.MediaType == mediaType);

        var totalCount = query.Count();

        var decks = query
                    .Include(d => d.Links)
                    .OrderBy(d => d.OriginalTitle)
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

        var frequencies = context.JmDictWordFrequencies.AsNoTracking().Where(f => wordIds.Contains(f.WordId)).ToList();

        List<WordDto> dto = new();

        foreach (var word in words)
        {
            if (word.jmDictWord == null)
            {
                continue;
            }

            var reading = word.jmDictWord.Readings[word.dw.ReadingIndex];
            // remove current and take the rest
            List<ReadingDto> alternativeReadings = word.jmDictWord.Readings.Where(r => r != reading)
                                                       .Select((r, i) => new ReadingDto
                                                                         {
                                                                             Text = r,
                                                                             ReadingIndex = i,
                                                                             ReadingType = word.jmDictWord.ReadingTypes[i],
                                                                             FrequencyRank =
                                                                                 frequencies.First(f => f.WordId == word.dw.WordId)
                                                                                            .ReadingsFrequencyRank[i],
                                                                             FrequencyPercentage =
                                                                                 frequencies.First(f => f.WordId == word.dw.WordId)
                                                                                            .ReadingsFrequencyPercentage[i]
                                                                         })
                                                       .ToList();

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

            var frequency = frequencies.First(f => f.WordId == word.dw.WordId);

            var mainReading = new ReadingDto()
                              {
                                  Text = reading,
                                  ReadingIndex = word.dw.ReadingIndex,
                                  ReadingType = word.jmDictWord.ReadingTypes[word.dw.ReadingIndex],
                                  FrequencyRank = frequency.ReadingsFrequencyRank[word.dw.ReadingIndex],
                                  FrequencyPercentage = frequency.ReadingsFrequencyPercentage[word.dw.ReadingIndex]
                              };


            var wordDto = new WordDto
                          {
                              WordId = word.jmDictWord.WordId,
                              MainReading = mainReading,
                              AlternativeReadings = alternativeReadings,
                              PartsOfSpeech = word.jmDictWord.PartsOfSpeech.ToHumanReadablePartsOfSpeech(),
                              Definitions = definitions
                          };

            dto.Add(wordDto);
        }

        return new PaginatedResponse<List<WordDto>>(dto, totalCount, pageSize, offset ?? 0);
    }
}