using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Jiten.Core.Data;
using Jiten.Core.Data.JMDict;
using Microsoft.EntityFrameworkCore;

namespace Jiten.Core;

public static class YomitanHelper
{
    /// <summary>
    /// Generates the content for the index.json file in a Yomitan dictionary.
    /// </summary>
    private static string GetIndexJson(MediaType? mediaType)
    {
        string title = mediaType != null ? $"Jiten ({mediaType})" : "Jiten";
        string revision = mediaType != null ? $"Jiten ({mediaType}) {DateTime.UtcNow:yy-MM-dd}" : $"Jiten {DateTime.UtcNow:yy-MM-dd}";
        string description = mediaType != null
            ? $"Dictionary based on frequency data of {mediaType} from jiten.moe"
            : "Dictionary based on frequency data of all media from jiten.moe";

        return
            $$"""{"title":"{{title}}","format":3,"revision":"{{revision}}","sequenced":false,"frequencyMode":"rank-based","author":"Jiten","url":"https://jiten.moe","description":"{{description}}"}""";
    }

    /// <summary>
    /// Generates a zipped Yomitan frequency dictionary for a given media type.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="mediaType">The media type to generate the frequency deck for. null for global</param>
    /// <returns>A byte array representing the zipped dictionary file.</returns>
    public static async Task<byte[]> GenerateYomitanFrequencyDeck(DbContextOptions<JitenDbContext> options,
                                                                  List<JmDictWordFrequency> frequencies, MediaType? mediaType)
    {
        await using var context = new JitenDbContext(options);

        var indexJson = GetIndexJson(mediaType);

        var wordIds = frequencies.Select(f => f.WordId).ToList();
        var allWords = await context.JMDictWords.AsNoTracking()
                                    .Where(w => wordIds.Contains(w.WordId))
                                    .ToDictionaryAsync(w => w.WordId);

        var yomitanTermList = new List<List<object>>();
        var addedEntries = new HashSet<string>(); // Prevents adding duplicate (term, reading) pairs

        foreach (var freq in frequencies)
        {
            if (!allWords.TryGetValue(freq.WordId, out JmDictWord? word)) continue;
            
            // Find the best (most frequent) kana reading to use as the default for kanji forms
            string? bestKanaReading = null;
            int bestKanaRank = int.MaxValue;

            for (int i = 0; i < word.Readings.Count; i++)
            {
                if (word.ReadingTypes[i] != JmDictReadingType.KanaReading) continue;
                if (freq.ReadingsFrequencyRank[i] >= bestKanaRank) continue;
                bestKanaRank = freq.ReadingsFrequencyRank[i];
                bestKanaReading = word.Readings[i];
            }

            // Now, iterate through all readings and create the appropriate entries
            for (int i = 0; i < word.Readings.Count; i++)
            {
                // Skip if this specific reading has a frequency of 0 in the selected media
                if (freq.ReadingsUsedInMediaAmount[i] == 0) continue;

                var currentTerm = word.Readings[i];
                var currentRank = freq.ReadingsFrequencyRank[i];
                var currentType = word.ReadingTypes[i];

                // Case 1: The reading is a Kanji form
                if (currentType == JmDictReadingType.Reading)
                {
                    // A kanji form needs a kana reading to be valid in Yomitan freq lists.
                    // We use the best one we found earlier.
                    if (bestKanaReading == null) continue;

                    string entryKey = $"{currentTerm}:{bestKanaReading}";
                    if (addedEntries.Contains(entryKey)) continue;

                    yomitanTermList.Add(new List<object>
                                        {
                                            currentTerm, "freq",
                                            new
                                            {
                                                reading = bestKanaReading,
                                                frequency = new { value = currentRank, displayValue = currentRank.ToString() }
                                            }
                                        });
                    addedEntries.Add(entryKey);
                }
                // Case 2: The reading is a Kana form
                else
                {
                    if (addedEntries.Contains(currentTerm)) continue;

                    yomitanTermList.Add([currentTerm, "freq", new { value = currentRank, displayValue = currentRank.ToString() }]);
                    addedEntries.Add(currentTerm);
                }
            }
        }

        var termBankJson = JsonSerializer.Serialize(yomitanTermList,
                                                    new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

        // 3. Zip the index and term bank files into a memory stream
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var indexEntry = archive.CreateEntry("index.json", CompressionLevel.Optimal);
            await using (var entryStream = indexEntry.Open())
            await using (var streamWriter = new StreamWriter(entryStream, Encoding.UTF8))
            {
                await streamWriter.WriteAsync(indexJson);
            }

            var termBankEntry = archive.CreateEntry("term_meta_bank_1.json", CompressionLevel.Optimal);
            await using (var entryStream = termBankEntry.Open())
            await using (var streamWriter = new StreamWriter(entryStream, Encoding.UTF8))
            {
                await streamWriter.WriteAsync(termBankJson);
            }
        }

        return memoryStream.ToArray();
    }
}