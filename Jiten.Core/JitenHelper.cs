using System.Diagnostics;
using Jiten.Core.Data;
using Jiten.Core.Data.JMDict;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Jiten.Core;

public static class JitenHelper
{
    public static async Task InsertDeck(Deck deck)
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
    public static async Task ComputeDifficulty()
    {
        int wordDifficultyWeight = 50;
        int characterCountWeight = 5;
        int uniqueWordCountWeight = 20;
        int uniqueKanjiCountWeight = 20;
        int averageSentenceLengthWeight = 5;

        int batchSize = 10;
        await using var context = new JitenDbContext();

        var allDecks = await context.Decks.ToListAsync();
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
                wordDifficultiesTotal.Add(deck.DeckId, 0);
                foreach (var word in deckWords.Where(dw => dw.DeckId == deck.DeckId))
                {
                    var frequencyRank = wordFrequencies[word.WordId].ReadingsFrequencyRank[word.ReadingIndex];
                    wordDifficultiesTotal[word.DeckId] += ((float)word.Occurrences / deck.WordCount) *
                                                          Math.Log2(Math.Truncate(frequencyRank / 2000f) + 1);
                }
            }
        }

        // Take the min and max values among all the decks
        double minDifficulty = wordDifficultiesTotal.Values.Min();
        double maxDifficulty = wordDifficultiesTotal.Values.Max();
        uint minCharacterCount = (uint)allDecks.Min(d => d.CharacterCount);
        uint maxCharacterCount = (uint)allDecks.Max(d => d.CharacterCount);
        uint minUniqueWordCount = (uint)allDecks.Min(d => d.UniqueWordCount);
        uint maxUniqueWordCount = (uint)allDecks.Max(d => d.UniqueWordCount);
        uint minUniqueKanjiCount = (uint)allDecks.Min(d => d.UniqueKanjiCount);
        uint maxUniqueKanjiCount = (uint)allDecks.Max(d => d.UniqueKanjiCount);
        double minAverageSentenceLength = allDecks.Min(d => d.AverageSentenceLength);
        double maxAverageSentenceLength = allDecks.Max(d => d.AverageSentenceLength);

        foreach (var deck in allDecks)
        {
            float difficulty = 0;
            difficulty += MapDoubleToWeight(wordDifficultiesTotal[deck.DeckId], minDifficulty, maxDifficulty, wordDifficultyWeight);
            difficulty += MapToWeight((uint)deck.CharacterCount, minCharacterCount, maxCharacterCount, characterCountWeight);
            difficulty += MapToWeight((uint)deck.UniqueWordCount, minUniqueWordCount, maxUniqueWordCount, uniqueWordCountWeight);
            difficulty += MapToWeight((uint)deck.UniqueKanjiCount, minUniqueKanjiCount, maxUniqueKanjiCount, uniqueKanjiCountWeight);
            difficulty += MapDoubleToWeight(deck.AverageSentenceLength, minAverageSentenceLength, maxAverageSentenceLength,
                                            averageSentenceLengthWeight);

            deck.Difficulty = (int)Math.Round(difficulty);
        }

        await context.SaveChangesAsync();

        // Map to the weight in a linear way
        float MapToWeight(uint value, uint min, uint max, int weight)
        {
            return (value - min) / (float)(max - min) * weight;
        }

        float MapDoubleToWeight(double value, double min, double max, int weight)
        {
            return (float)((value - min) / (max - min)) * weight;
        }
    }
}