using AnkiNet;
using Jiten.Api.Dtos;
using Jiten.Api.Enums;
using Jiten.Api.Helpers;
using Jiten.Core;
using Jiten.Core.Data;
using Jiten.Core.Data.JMDict;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jiten.Api.Controllers;

[ApiController]
[Route("api/media-deck")]
public class MediaDeckController(JitenDbContext context) : ControllerBase
{
    [HttpGet("get-media-decks")]
    public PaginatedResponse<List<Deck>> GetMediaDecks(int? offset = 0, MediaType? mediaType = null,
                                                       int wordId = 0, int readingIndex = 0, string? titleFilter = "", string? sortBy = "",
                                                       SortOrder sortOrder = SortOrder.Ascending)
    {
        int pageSize = 50;

        var query = context.Decks.AsNoTracking();

        query = query.Where(d => d.ParentDeck == null);
        if (mediaType != null)
            query = query.Where(d => d.MediaType == mediaType);

        if (!string.IsNullOrEmpty(titleFilter))
        {
            query = query.Where(d =>
                                    // Fuzzy matching
                                    EF.Functions.FuzzyStringMatchLevenshtein(d.OriginalTitle, titleFilter) <= 3 ||
                                    EF.Functions.FuzzyStringMatchLevenshtein(d.RomajiTitle, titleFilter) <= 3 ||
                                    EF.Functions.FuzzyStringMatchLevenshtein(d.EnglishTitle, titleFilter) <= 3 ||

                                    // Substring matching
                                    EF.Functions.ILike(d.OriginalTitle, $"%{titleFilter}%") ||
                                    EF.Functions.ILike(d.RomajiTitle, $"%{titleFilter}%") ||
                                    EF.Functions.ILike(d.EnglishTitle, $"%{titleFilter}%")
                               );
        }

        if (wordId != 0)
            query = query.Where(d => d.DeckWords.Any(dw => dw.WordId == wordId && dw.ReadingIndex == readingIndex));

        var totalCount = query.Count();

        if (string.IsNullOrEmpty(sortBy))
            sortBy = "title";

        query = sortBy switch
        {
            "difficulty" => sortOrder == SortOrder.Ascending
                ? query.OrderBy(d => d.Difficulty)
                : query.OrderByDescending(d => d.Difficulty),
            "charCount" => sortOrder == SortOrder.Ascending
                ? query.OrderBy(d => d.CharacterCount)
                : query.OrderByDescending(d => d.CharacterCount),
            "sentenceLength" => sortOrder == SortOrder.Ascending
                ? query.OrderBy(d => d.CharacterCount / (d.SentenceCount + 1))
                : query.OrderByDescending(d => d.CharacterCount / (d.SentenceCount + 1)),
            "wordCount" => sortOrder == SortOrder.Ascending
                ? query.OrderBy(d => d.WordCount)
                : query.OrderByDescending(d => d.WordCount),
            "uKanji" => sortOrder == SortOrder.Ascending
                ? query.OrderBy(d => d.UniqueKanjiCount)
                : query.OrderByDescending(d => d.UniqueKanjiCount),
            "uWordCount" => sortOrder == SortOrder.Ascending
                ? query.OrderBy(d => d.UniqueWordCount)
                : query.OrderByDescending(d => d.UniqueWordCount),
            "uKanjiOnce" => sortOrder == SortOrder.Ascending
                ? query.OrderBy(d => d.UniqueKanjiUsedOnceCount)
                : query.OrderByDescending(d => d.UniqueKanjiUsedOnceCount),
            _ => sortOrder == SortOrder.Ascending
                ? query.OrderBy(d => d.RomajiTitle)
                : query.OrderByDescending(d => d.RomajiTitle),
        };

        var decks = query
                    .Include(d => d.Links)
                    .Skip(offset ?? 0)
                    .Take(pageSize)
                    .ToList();

        return new PaginatedResponse<List<Deck>>(decks, totalCount, pageSize, offset ?? 0);
    }

    [HttpGet("{id}/vocabulary")]
    public PaginatedResponse<DeckVocabularyListDto> GetVocabulary(int id, string? sortOrder = "", int? offset = 0)
    {
        int pageSize = 100;

        var deck = context.Decks.AsNoTracking().FirstOrDefault(d => d.DeckId == id);

        int totalCount = context.DeckWords.AsNoTracking().Count(dw => dw.DeckId == id);

        var deckWords = context.DeckWords.AsNoTracking()
                               .Where(dw => dw.DeckId == id)
                               .OrderBy(dw => dw.DeckWordId)
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

        DeckVocabularyListDto dto = new() { DeckId = id, Title = deck.OriginalTitle, Words = new() };

        foreach (var word in words)
        {
            if (word.jmDictWord == null)
            {
                continue;
            }

            var reading = word.jmDictWord.Readings[word.dw.ReadingIndex];

            List<ReadingDto> alternativeReadings = word.jmDictWord.Readings
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

            // Remove current
            alternativeReadings.RemoveAt(word.dw.ReadingIndex);

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
                              Definitions = word.jmDictWord.Definitions.ToDefinitionDtos()
                          };

            dto.Words.Add(wordDto);
        }

        return new PaginatedResponse<DeckVocabularyListDto>(dto, totalCount, pageSize, offset ?? 0);
    }

    [HttpGet("{id}/download")]
    public async Task<IResult> DownloadDeck(int id, DeckFormat format, DeckDownloadType downloadType, DeckOrder order,
                                            int minFrequency = 0, int maxFrequency = 0)
    {
        var deck = context.Decks.AsNoTracking().FirstOrDefault(d => d.DeckId == id);

        if (deck == null)
        {
            return Results.NotFound();
        }

        IQueryable<DeckWord> deckWordsQuery = context.DeckWords.AsNoTracking().Where(dw => dw.DeckId == id);

        switch (downloadType)
        {
            case DeckDownloadType.Full:
                break;

            case DeckDownloadType.TopGlobalFrequency:
                var frequencies = context.JmDictWordFrequencies.AsNoTracking().OrderBy(w => w.FrequencyRank).ToDictionary(w => w.WordId);
                // Get the words between frequency rank minFrequency and maxFrequency
                deckWordsQuery = deckWordsQuery.Where(dw => context.JmDictWordFrequencies
                                                                   .Any(f => f.WordId == dw.WordId &&
                                                                             f.ReadingsFrequencyRank[dw.ReadingIndex] >= minFrequency &&
                                                                             f.ReadingsFrequencyRank[dw.ReadingIndex] <= maxFrequency));
                break;

            case DeckDownloadType.TopDeckFrequency:
                deckWordsQuery = deckWordsQuery
                                 .OrderBy(dw => dw.Occurrences)
                                 .Skip(minFrequency)
                                 .Take(maxFrequency - minFrequency);
                break;

            case DeckDownloadType.TopChronological:
                deckWordsQuery = deckWordsQuery
                                 .OrderBy(dw => dw.DeckWordId)
                                 .Skip(minFrequency)
                                 .Take(maxFrequency - minFrequency);
                break;
            default:
                return Results.BadRequest();
        }

        switch (order)
        {
            case DeckOrder.Chronological:
                deckWordsQuery = deckWordsQuery.OrderBy(dw => dw.DeckWordId);
                break;

            case DeckOrder.GlobalFrequency:
                deckWordsQuery = deckWordsQuery.OrderBy(dw => context.JmDictWordFrequencies
                                                                     .Where(f => f.WordId == dw.WordId)
                                                                     .Select(f => f.ReadingsFrequencyRank[dw.ReadingIndex])
                                                                     .FirstOrDefault()
                                                       );
                break;

            case DeckOrder.DeckFrequency:
                deckWordsQuery = deckWordsQuery.OrderBy(dw => dw.Occurrences);
                break;
            default:
                return Results.BadRequest();
        }

        var wordIds = await deckWordsQuery.Select(dw => dw.WordId).Distinct().ToListAsync();
        var deckWords = await deckWordsQuery.Select(dw => new { dw.WordId, dw.ReadingIndex }).Distinct().ToListAsync();

        var jmdictWords = await context.JMDictWords.AsNoTracking()
                                       .Include(w => w.Definitions)
                                       .Where(w => wordIds.Contains(w.WordId))
                                       .ToDictionaryAsync(w => w.WordId);

        switch (format)
        {
            case DeckFormat.Anki:
                var cardTypes = new[]
                                {
                                    new AnkiCardType(
                                                     "Forwards",
                                                     0,
                                                     "{{Front}}",
                                                     "{{Front}}<hr id=\"answer\">{{Back}}"
                                                    ),
                                };

                var noteType = new AnkiNoteType(
                                                "Basic (With hints)",
                                                cardTypes,
                                                new[] { "Front", "Back" }
                                               );

                var collection = new AnkiCollection();
                var noteTypeId = collection.CreateNoteType(noteType);
                var deckId = collection.CreateDeck(deck.OriginalTitle);

                foreach (var word in deckWords)
                {
                    collection.CreateNote(deckId, noteTypeId, jmdictWords[word.WordId].Readings[word.ReadingIndex],
                                          String.Join(",", jmdictWords[word.WordId].Definitions.SelectMany(d => d.EnglishMeanings)));
                }

                var stream = new MemoryStream();

                await AnkiFileWriter.WriteToStreamAsync(stream, collection);
                var bytes = stream.ToArray();

                return Results.File(bytes, "application/x-binary", $"{deck.OriginalTitle}.apkg");
            case DeckFormat.Csv:
                break;
            default:
                return Results.BadRequest();
        }

        return Results.BadRequest();
    }
}