using Jiten.Core.Data;
using Jiten.Core.Data.JMDict;
using Microsoft.EntityFrameworkCore;

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
        context.JmDictWordFrequencies.RemoveRange(context.JmDictWordFrequencies);
        await context.SaveChangesAsync();

        Dictionary<int, JmDictWordFrequency> wordFrequencies = new();

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
                var word = wordFrequencies.TryGetValue(deckWord.WordId, out var wordFrequency)
                    ? wordFrequency
                    : new JmDictWordFrequency
                      {
                          WordId = deckWord.WordId,
                          FrequencyRank = 0,
                          UsedInMediaAmount = 0,
                          ReadingsFrequencyPercentage = [],
                          KanaReadingsFrequencyPercentage = [],
                          ReadingsFrequencyRank = [],
                          KanaReadingsFrequencyRank = [],
                          ReadingsUsedInMediaAmount = [],
                          KanaReadingsUsedInMediaAmount = [],
                      };

                word.FrequencyRank += deckWord.Occurrences;
                word.UsedInMediaAmount++;

                if (deckWord.ReadingType == 0)
                {
                    while (word.ReadingsUsedInMediaAmount.Count <= deckWord.ReadingIndex)
                    {
                        word.ReadingsUsedInMediaAmount.Add(0);
                        word.ReadingsFrequencyPercentage.Add(0);
                        word.ReadingsFrequencyRank.Add(0);
                    }

                    word.ReadingsUsedInMediaAmount[deckWord.ReadingIndex]++;
                    word.ReadingsFrequencyRank[deckWord.ReadingIndex] += deckWord.Occurrences;
                }
                else
                {
                    while (word.KanaReadingsUsedInMediaAmount.Count <= deckWord.ReadingIndex)
                    {
                        word.KanaReadingsUsedInMediaAmount.Add(0);
                        word.KanaReadingsFrequencyPercentage.Add(0);
                        word.KanaReadingsFrequencyRank.Add(0);
                    }

                    word.KanaReadingsUsedInMediaAmount[deckWord.ReadingIndex]++;
                    word.KanaReadingsFrequencyRank[deckWord.ReadingIndex] += deckWord.Occurrences;
                }

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
                word.ReadingsFrequencyPercentage[j] = word.ReadingsFrequencyRank[j] / (double)word.FrequencyRank * 100;
            }

            for (int j = 0; j < word.KanaReadingsUsedInMediaAmount.Count; j++)
            {
                word.KanaReadingsFrequencyPercentage[j] = word.KanaReadingsFrequencyRank[j] / (double)word.FrequencyRank * 100;
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
            allReadings.AddRange(wordFreq.KanaReadingsFrequencyRank);
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

            for (int i = 0; i < wordFreq.KanaReadingsFrequencyRank.Count; i++)
            {
                int frequency = wordFreq.KanaReadingsFrequencyRank[i];
                wordFreq.KanaReadingsFrequencyRank[i] = frequencyRanks[frequency];
            }
        }

        batchSize = 10000;

        for (int i = 0; i < sortedWordFrequencies.Count; i += batchSize)
        {
            var batch = sortedWordFrequencies.Skip(i).Take(batchSize);
            await context.JmDictWordFrequencies.AddRangeAsync(batch);
            await context.SaveChangesAsync();
        }
    }
}