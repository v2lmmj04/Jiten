using System.Diagnostics;
using Jiten.Core.Data;

namespace Jiten.Parser;

public static class ExampleSentenceExtractor
{
    private const int FIRST_PASS_MIN_LENGTH = 15;
    private const int FIRST_PASS_MAX_LENGTH = 40;
    private const float FIRST_PASS_PERCENTAGE = 0.25f;

    private const int SECOND_PASS_MIN_LENGTH = 10;
    private const int SECOND_PASS_MAX_LENGTH = 45;
    private const float SECOND_PASS_PERCENTAGE = 0.5f;

    private const int THIRD_PASS_MIN_LENGTH = 10;
    private const int THIRD_PASS_MAX_LENGTH = 55;
    private const float THIRD_PASS_PERCENTAGE = 1f;

    public static List<ExampleSentence> ExtractSentences(List<SentenceInfo> sentences, DeckWord[] words)
    {
        // Pre-filter sentences with insufficient character diversity
        var validSentences = new List<SentenceInfo>(sentences.Count);
        for (int i = 0; i < sentences.Count; i++)
        {
            var sentence = sentences[i];
            var distinctChars = new HashSet<char>();
            foreach (var (wordInfo, _, _) in sentence.Words)
            {
                foreach (char c in wordInfo.Text)
                {
                    distinctChars.Add(c);
                    if (distinctChars.Count >= 6) break;
                }
                if (distinctChars.Count >= 6) break;
            }
            
            if (distinctChars.Count >= 6)
            {
                validSentences.Add(sentence);
            }
        }

        // Create position lookup for valid sentences only
        var sentencePositions = new Dictionary<SentenceInfo, int>(validSentences.Count);
        for (int i = 0; i < sentences.Count; i++)
        {
            if (validSentences.Contains(sentences[i]))
            {
                sentencePositions[sentences[i]] = i;
            }
        }

        // Group words by text for O(1) lookup instead of linear search
        var wordsByText = new Dictionary<string, Queue<DeckWord>>();
        foreach (var word in words)
        {
            if (!wordsByText.ContainsKey(word.OriginalText))
            {
                wordsByText[word.OriginalText] = new Queue<DeckWord>();
            }
            wordsByText[word.OriginalText].Enqueue(new DeckWord 
            { 
                WordId = word.WordId, 
                ReadingIndex = word.ReadingIndex, 
                OriginalText = word.OriginalText, 
                PartsOfSpeech = word.PartsOfSpeech 
            });
        }

        var exampleSentences = new List<ExampleSentence>();
        var usedSentences = new HashSet<SentenceInfo>();

        var passes = new[]
        {
            new { MinLength = FIRST_PASS_MIN_LENGTH, MaxLength = FIRST_PASS_MAX_LENGTH, Percentage = FIRST_PASS_PERCENTAGE },
            new { MinLength = SECOND_PASS_MIN_LENGTH, MaxLength = SECOND_PASS_MAX_LENGTH, Percentage = SECOND_PASS_PERCENTAGE },
            new { MinLength = THIRD_PASS_MIN_LENGTH, MaxLength = THIRD_PASS_MAX_LENGTH, Percentage = THIRD_PASS_PERCENTAGE }
        };

        foreach (var pass in passes)
        {
            int maxSentences = (int)(validSentences.Count * pass.Percentage);
            int processed = 0;

            // Pre-filter and sort sentences for this pass
            var candidateSentences = new List<SentenceInfo>();
            foreach (var sentence in validSentences)
            {
                if (!usedSentences.Contains(sentence) &&
                    sentence.Text.Length >= pass.MinLength &&
                    sentence.Text.Length <= pass.MaxLength)
                {
                    candidateSentences.Add(sentence);
                }
            }

            // Sort by length descending
            candidateSentences.Sort((a, b) => b.Text.Length.CompareTo(a.Text.Length));

            // Process up to maxSentences
            int toProcess = Math.Min(candidateSentences.Count, maxSentences - processed);
            for (int i = 0; i < toProcess; i++)
            {
                var sentence = candidateSentences[i];
                var exampleSentence = new ExampleSentence
                {
                    Text = sentence.Text,
                    Position = sentencePositions[sentence],
                    Words = new List<ExampleSentenceWord>()
                };

                bool foundAnyWord = false;

                foreach (var (wordInfo, position, length) in sentence.Words)
                {
                    if (wordsByText.TryGetValue(wordInfo.Text, out var wordQueue) && wordQueue.Count > 0)
                    {
                        var foundWord = wordQueue.Dequeue();
                        
                        exampleSentence.Words.Add(new ExampleSentenceWord
                        {
                            WordId = foundWord.WordId,
                            ReadingIndex = foundWord.ReadingIndex,
                            Position = position,
                            Length = length
                        });

                        foundAnyWord = true;

                        // Remove empty queues to avoid future lookups
                        if (wordQueue.Count == 0)
                        {
                            wordsByText.Remove(wordInfo.Text);
                        }
                    }
                }

                if (foundAnyWord)
                {
                    exampleSentences.Add(exampleSentence);
                }

                usedSentences.Add(sentence);
                processed++;

                // Early exit if no more words available
                if (wordsByText.Count == 0)
                {
                    return exampleSentences;
                }
            }

            // Early exit if no more words available
            if (wordsByText.Count == 0)
            {
                break;
            }
        }

        return exampleSentences;
    }
}