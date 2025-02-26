using System.Diagnostics;
using BunnyCDN.Net.Storage;
using Jiten.Core.Data;
using Jiten.Core.Data.JMDict;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Jiten.Core;

public static class JitenHelper
{
    public static async Task InsertDeck(Deck deck, byte[] cover)
    {
        // Ignore if the deck already exists
        await using var context = new JitenDbContext();

        if (await context.Decks.AnyAsync(d => d.OriginalTitle == deck.OriginalTitle && d.MediaType == deck.MediaType))
            return;

        // Fix potential null references to decks
        deck.SetParentsAndDeckWordDeck(deck);
        deck.ParentDeckId = null;

        context.Decks.Add(deck);

        await context.SaveChangesAsync();

        // var coverUrl = await BunnyCdnHelper.UploadFile(cover, $"{deck.DeckId}/cover.jpg");
        // deck.CoverName = coverUrl;
        //
        // await context.SaveChangesAsync();
    }

    public static async Task ComputeFrequencies()
    {
        int batchSize = 100000;
        await using var context = new JitenDbContext();

        // Delete previous frequency data if it exists
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE jmdict.\"WordFrequencies\"");

        Dictionary<int, JmDictWordFrequency> wordFrequencies = new();

        // Prefill the dictionary with default values
        var allWords = await context.JMDictWords.AsNoTracking().ToListAsync();

        foreach (var word in allWords)
        {
            var wordFrequency = new JmDictWordFrequency
                                {
                                    WordId = word.WordId,
                                    FrequencyRank = 0,
                                    UsedInMediaAmount = 0,
                                    ReadingsFrequencyPercentage = [..new float[word.Readings.Count]],
                                    ReadingsObservedFrequency = [..new double[word.Readings.Count]],
                                    ReadingsFrequencyRank = [..new int[word.Readings.Count]],
                                    ReadingsUsedInMediaAmount = [..new int[word.Readings.Count]],
                                };

            wordFrequencies.Add(word.WordId, wordFrequency);
        }

        var primaryDecks = await context.Decks.AsNoTracking()
                                        .Where(d => d.ParentDeck == null)
                                        .Select(d => d.DeckId)
                                        .ToListAsync();

        int totalEntries = await context.DeckWords.Where(d => primaryDecks.Contains(d.DeckId)).CountAsync();
        var processedDecks = new HashSet<(int, int)>();

        for (int i = 0; i < totalEntries; i += batchSize)
        {
            var deckWords = await context.DeckWords
                                         .AsNoTracking()
                                         .Where(d => primaryDecks.Contains(d.DeckId))
                                         .Skip(i)
                                         .Take(batchSize)
                                         .ToListAsync();

            foreach (var deckWord in deckWords)
            {
                var word = wordFrequencies[deckWord.WordId];

                word.FrequencyRank += deckWord.Occurrences;

                // The word itself must be only counted once per deck
                if (!processedDecks.Contains((deckWord.DeckId, deckWord.WordId)))
                {
                    word.UsedInMediaAmount++;
                    processedDecks.Add((deckWord.DeckId, deckWord.WordId));
                }

                word.ReadingsUsedInMediaAmount[deckWord.ReadingIndex]++;
                word.ReadingsFrequencyRank[deckWord.ReadingIndex] += deckWord.Occurrences;

                wordFrequencies.TryAdd(deckWord.WordId, word);
            }
        }

        var sortedWordFrequencies = wordFrequencies.Values
                                                   .OrderByDescending(w => w.FrequencyRank)
                                                   .ToList();

        int currentRank = 0;
        int previousFrequencyRank = sortedWordFrequencies.FirstOrDefault()?.FrequencyRank ?? 0;
        int duplicateCount = 0;
        int totalOccurences = sortedWordFrequencies.Sum(w => w.FrequencyRank);

        for (int i = 0; i < sortedWordFrequencies.Count; i++)
        {
            var word = sortedWordFrequencies[i];

            for (int j = 0; j < word.ReadingsUsedInMediaAmount.Count; j++)
            {
                word.ReadingsObservedFrequency[j] = word.ReadingsFrequencyRank[j] / (double)totalOccurences;
                word.ReadingsFrequencyPercentage[j] = (word.ReadingsFrequencyRank[j] / (double)word.FrequencyRank) * 100;
            }

            // Keep the same rank if they have the same amount of occurences

            if (word.FrequencyRank == previousFrequencyRank)
            {
                duplicateCount++;
            }
            else
            {
                currentRank += duplicateCount;
                duplicateCount = 1;
                previousFrequencyRank = word.FrequencyRank;
            }

            word.ObservedFrequency = word.FrequencyRank / (double)totalOccurences;
            word.FrequencyRank = currentRank + 1;
        }

        var allReadings = new List<int>();

        foreach (var wordFreq in sortedWordFrequencies)
        {
            allReadings.AddRange(wordFreq.ReadingsFrequencyRank);
        }

        // Sort by occurences and group the readings with the same amount of occurences so we can skip ranks 
        var frequencyGroups = allReadings
                              .GroupBy(x => x)
                              .OrderByDescending(g => g.Key)
                              .ToList();

        var frequencyRanks = new Dictionary<int, int>();
        currentRank = 1;

        // Assign a rank for each amount of occurence, skip ranks based on number of duplicates
        foreach (var group in frequencyGroups)
        {
            frequencyRanks[group.Key] = currentRank;
            currentRank += group.Count();
        }

        foreach (var wordFreq in sortedWordFrequencies)
        {
            for (int i = 0; i < wordFreq.ReadingsFrequencyRank.Count; i++)
            {
                int frequency = wordFreq.ReadingsFrequencyRank[i];
                wordFreq.ReadingsFrequencyRank[i] = frequencyRanks[frequency];
            }
        }

        // Bulk insert using PostgreSQL COPY command
        const string tempTable = "temp_word_frequencies";

        // Create a new connection for the COPY operation
        await using var conn = new NpgsqlConnection(context.Database.GetConnectionString());
        await conn.OpenAsync();

        // First, retrieve the structure of the target table
        var command = new NpgsqlCommand(@"
        SELECT string_agg(column_name || ' ' || data_type, ', ' ORDER BY ordinal_position)
        FROM information_schema.columns
        WHERE table_name = 'WordFrequencies'
          AND column_name != 'id'
    ", conn);

        string tableStructure = (string)await command.ExecuteScalarAsync();

        // Create temp table with exact same structure
        await using (var cmd = new NpgsqlCommand($@"
        CREATE TEMP TABLE {tempTable} AS 
        SELECT * FROM jmdict.""WordFrequencies"" WHERE 1=0;", conn))
        {
            await cmd.ExecuteNonQueryAsync();
        }


        // Perform the COPY operation
        await using (var writer =
                     await
                         conn.BeginBinaryImportAsync($@"COPY {tempTable} (""WordId"", ""FrequencyRank"", ""ObservedFrequency"", ""UsedInMediaAmount"", ""ReadingsFrequencyPercentage"", ""ReadingsObservedFrequency"", ""ReadingsFrequencyRank"", ""ReadingsUsedInMediaAmount"") FROM STDIN (FORMAT BINARY)"))
        {
            foreach (var word in sortedWordFrequencies)
            {
                await writer.StartRowAsync();
                await writer.WriteAsync(word.WordId);
                await writer.WriteAsync(word.FrequencyRank);
                await writer.WriteAsync(word.ObservedFrequency);
                await writer.WriteAsync(word.UsedInMediaAmount);
                await writer.WriteAsync(word.ReadingsFrequencyPercentage, NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Double);
                await writer.WriteAsync(word.ReadingsObservedFrequency, NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Double);
                await writer.WriteAsync(word.ReadingsFrequencyRank, NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer);
                await writer.WriteAsync(word.ReadingsUsedInMediaAmount, NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer);
            }

            await writer.CompleteAsync();
        }

        // Insert from temp table to final table and cleanup
        await using (var cmd = new NpgsqlCommand($@"
        INSERT INTO jmdict.""WordFrequencies""
        SELECT * FROM {tempTable};
        DROP TABLE {tempTable};", conn))
        {
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Calculate the difficulty of decks. Still very WIP
    /// </summary>
    public static async Task ComputeDifficulty(bool verbose)
    {
        int wordDifficultyWeight = 60;
        int characterCountWeight = 5;
        int uniqueWordCountWeight = 10;
        int uniqueKanjiCountWeight = 20;
        int averageSentenceLengthWeight = 5;

        int batchSize = 10;
        await using var context = new JitenDbContext();

        //TODO : compute the difficulty independently for each media type

        var allDecks = await context.Decks.Where(d => d.MediaType == MediaType.VisualNovel).ToListAsync();
        var allFrequencies = await context.JmDictWordFrequencies.AsNoTracking().ToListAsync();
        var wordFrequencies = allFrequencies.ToDictionary(f => f.WordId, f => f);

        Dictionary<int, double> wordDifficultiesTotal = new();

        // Compute the word difficulty for each deck, based on the frequency rank of each words present in it
        for (int i = 0; i < allDecks.Count; i += batchSize)
        {
            var decks = allDecks.Skip(i).Take(batchSize).ToList();
            var deckWords = await context.DeckWords.AsNoTracking().Where(dw => decks.Select(d => d.DeckId).Contains(dw.DeckId))
                                         .ToListAsync();

            foreach (var deck in decks)
            {
                Dictionary<int, double> wordFrequencyBands = new Dictionary<int, double>()
                                                             {
                                                                 { 1, 0 }, // Common (0-2000)
                                                                 { 2, 0 }, // Uncommon (2001-10000)
                                                                 { 3, 0 }, // Rare (10001-25000)
                                                                 { 4, 0 }, // Very rare (25001-40000)
                                                                 { 5, 0 } // Extremely rare (40001+)
                                                             };

                // Group words by ID and reading to get unique words with their occurrences
                var wordGroups = deckWords
                                 .Where(dw => dw.DeckId == deck.DeckId)
                                 .GroupBy(dw => new { dw.WordId, dw.ReadingIndex })
                                 .Select(g => new
                                              {
                                                  WordId = g.Key.WordId,
                                                  ReadingIndex = g.Key.ReadingIndex,
                                                  Occurrences = g.Sum(dw => dw.Occurrences)
                                              })
                                 .ToList();

                double totalWeightedOccurrences = 0;

                foreach (var word in wordGroups)
                {
                    var frequencyRank = wordFrequencies[word.WordId].ReadingsFrequencyRank[word.ReadingIndex];

                    // Apply a logarithmic weighting to occurrences - words that appear many times 
                    // matter more, but with diminishing returns
                    double occurrenceWeight = Math.Log10(word.Occurrences + 1);
                    totalWeightedOccurrences += occurrenceWeight;

                    if (frequencyRank <= 2000)
                        wordFrequencyBands[1] += occurrenceWeight;
                    else if (frequencyRank <= 10000)
                        wordFrequencyBands[2] += occurrenceWeight;
                    else if (frequencyRank <= 25000)
                        wordFrequencyBands[3] += occurrenceWeight;
                    else if (frequencyRank <= 40000)
                        wordFrequencyBands[4] += occurrenceWeight;
                    else
                        wordFrequencyBands[5] += occurrenceWeight;
                }

                // Calculate difficulty based on weighted proportions
                wordDifficultiesTotal[deck.DeckId] =
                    (wordFrequencyBands[1] / totalWeightedOccurrences * 0.1) +
                    (wordFrequencyBands[2] / totalWeightedOccurrences * 0.3) +
                    (wordFrequencyBands[3] / totalWeightedOccurrences * 1.0) +
                    (wordFrequencyBands[4] / totalWeightedOccurrences * 2.0) +
                    (wordFrequencyBands[5] / totalWeightedOccurrences * 4.0);

                if (deck.DeckId == 65584 || deck.DeckId == 65580)
                    Debugger.Break();
                
                // Apply a vocabulary size scaling factor 
                // Increase the power (0.7) for more separation between large and small vocabularies
                wordDifficultiesTotal[deck.DeckId] *= Math.Pow(Math.Log10(deck.UniqueWordCount), 0.7) * 4;
            }
        }

        // Take the min and max values among all the decks
            // We go by 1st and 9th decile to avoid outliers
            // double minDifficulty = wordDifficultiesTotal.Values.Min();
            // double maxDifficulty = wordDifficultiesTotal.Values.Max();
            // uint minCharacterCount = (uint)allDecks.Min(d => d.CharacterCount);
            // uint maxCharacterCount = (uint)allDecks.Max(d => d.CharacterCount);
            // uint minUniqueWordCount = (uint)allDecks.Min(d => d.UniqueWordCount);
            // uint maxUniqueWordCount = (uint)allDecks.Max(d => d.UniqueWordCount);
            // uint minUniqueKanjiCount = (uint)allDecks.Min(d => d.UniqueKanjiCount);
            // uint maxUniqueKanjiCount = (uint)allDecks.Max(d => d.UniqueKanjiCount);
            // double minAverageSentenceLength = allDecks.Min(d => d.AverageSentenceLength);
            // double maxAverageSentenceLength = allDecks.Max(d => d.AverageSentenceLength);

            double minDifficulty = wordDifficultiesTotal.Values.OrderBy(d => d).ElementAt((int)(allDecks.Count * 0.05f));
            double maxDifficulty = wordDifficultiesTotal.Values.OrderBy(d => d).ElementAt((int)(allDecks.Count * 0.95f));
            uint minCharacterCount = (uint)allDecks.OrderBy(d => d.CharacterCount).ElementAt((int)(allDecks.Count * 0.05f)).CharacterCount;
            uint maxCharacterCount = (uint)allDecks.OrderBy(d => d.CharacterCount).ElementAt((int)(allDecks.Count * 0.95f)).CharacterCount;
            uint minUniqueWordCount = (uint)allDecks.OrderBy(d => d.UniqueWordCount).ElementAt((int)(allDecks.Count * 0.05f)).UniqueWordCount;
            uint maxUniqueWordCount = (uint)allDecks.OrderBy(d => d.UniqueWordCount).ElementAt((int)(allDecks.Count * 0.95f)).UniqueWordCount;
            uint minUniqueKanjiCount = (uint)allDecks.OrderBy(d => d.UniqueKanjiCount).ElementAt((int)(allDecks.Count * 0.05f)).UniqueKanjiCount;
            uint maxUniqueKanjiCount = (uint)allDecks.OrderBy(d => d.UniqueKanjiCount).ElementAt((int)(allDecks.Count * 0.95f)).UniqueKanjiCount;
            double minAverageSentenceLength =
                allDecks.OrderBy(d => d.AverageSentenceLength).ElementAt((int)(allDecks.Count * 0.05f)).AverageSentenceLength;
            double maxAverageSentenceLength =
                allDecks.OrderBy(d => d.AverageSentenceLength).ElementAt((int)(allDecks.Count * 0.95f)).AverageSentenceLength;
            
            foreach (var deck in allDecks)
            {
                double difficulty = 0;

                float wordDifficulty =
                    MapDoubleToWeight(wordDifficultiesTotal[deck.DeckId], minDifficulty, maxDifficulty, wordDifficultyWeight);
                float characterCountDifficulty =
                    MapToWeight((uint)deck.CharacterCount, minCharacterCount, maxCharacterCount, characterCountWeight);
                float uniqueWordCountDifficulty =
                    MapToWeight((uint)deck.UniqueWordCount, minUniqueWordCount, maxUniqueWordCount, uniqueWordCountWeight);
                float uniqueKanjiCountDifficulty =
                    MapToWeight((uint)deck.UniqueKanjiCount, minUniqueKanjiCount, maxUniqueKanjiCount, uniqueKanjiCountWeight);
                float averageSentenceLengthDifficulty = MapDoubleToWeight(deck.AverageSentenceLength, minAverageSentenceLength,
                                                                          maxAverageSentenceLength, averageSentenceLengthWeight);

                difficulty += wordDifficulty;
                difficulty += characterCountDifficulty;
                difficulty += uniqueWordCountDifficulty;
                difficulty += uniqueKanjiCountDifficulty;
                difficulty += averageSentenceLengthDifficulty;

                deck.Difficulty = (int)Math.Round(difficulty);

                if (verbose)
                {
                    Console.WriteLine("========");
                    Console.WriteLine("Difficulty for deck " + deck.RomajiTitle);
                    Console.WriteLine("Word difficulty:" + wordDifficulty);
                    Console.WriteLine("Character count difficulty:" + characterCountDifficulty);
                    Console.WriteLine("Unique word count difficulty:" + uniqueWordCountDifficulty);
                    Console.WriteLine("Unique kanji count difficulty:" + uniqueKanjiCountDifficulty);
                    Console.WriteLine("Average sentence length difficulty:" + averageSentenceLengthDifficulty);
                    Console.WriteLine("Total difficulty:" + deck.Difficulty);
                }
            }

            await context.SaveChangesAsync();

            // Map to the weight in a linear way
            float MapToWeight(uint value, uint min, uint max, int weight)
            {
                if (value < min)
                    return 0;

                if (value > max)
                    return weight;

                return (value - min) / (float)(max - min) * weight;
            }

            float MapDoubleToWeight(double value, double min, double max, int weight)
            {
                if (value < min)
                    return 0;

                if (value > max)
                    return weight;

                return (float)((value - min) / (max - min)) * weight;
            }
        }
    }