using Jiten.Core.Data;
using Jiten.Core.Data.JMDict;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

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
                                    ReadingsFrequencyRank = [..new int[word.Readings.Count]],
                                    ReadingsUsedInMediaAmount = [..new int[word.Readings.Count]],
                                };

            wordFrequencies.Add(word.WordId, wordFrequency);
        }

        int totalEntries = await context.DeckWords.CountAsync();
        for (int i = 0; i < totalEntries; i += batchSize)
        {
            var deckWords = await context.DeckWords
                                         .AsNoTracking()
                                         .Skip(i)
                                         .Take(batchSize)
                                         .ToListAsync();

            foreach (var deckWord in deckWords)
            {
                var word = wordFrequencies[deckWord.WordId];

                word.FrequencyRank += deckWord.Occurrences;
                word.UsedInMediaAmount++;
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

        for (int i = 0; i < sortedWordFrequencies.Count; i++)
        {
            var word = sortedWordFrequencies[i];

            for (int j = 0; j < word.ReadingsUsedInMediaAmount.Count; j++)
            {
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
                         conn.BeginBinaryImportAsync($@"COPY {tempTable} (""WordId"", ""FrequencyRank"", ""UsedInMediaAmount"", ""ReadingsFrequencyPercentage"", ""ReadingsFrequencyRank"", ""ReadingsUsedInMediaAmount"") FROM STDIN (FORMAT BINARY)"))
        {
            foreach (var word in sortedWordFrequencies)
            {
                await writer.StartRowAsync();
                await writer.WriteAsync(word.WordId);
                await writer.WriteAsync(word.FrequencyRank);
                await writer.WriteAsync(word.UsedInMediaAmount);
                await writer.WriteAsync(word.ReadingsFrequencyPercentage, NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Double);
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
}