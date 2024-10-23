using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Jiten.Core;
using Jiten.Core.Data;
using Jiten.Core.Data.JMDict;
using WanaKanaShaapu;

namespace Jiten.Parser
{
    public static class Program
    {
        private static Dictionary<string, List<int>> _lookups = new();
        private static Dictionary<int, JmDictWord> _allWords = new();
        private static bool _initialized = false;
        private static readonly SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1);

        public static async Task Main(string[] args)
        {
            var text = await File.ReadAllTextAsync(@"Y:\00_JapaneseStudy\JL\Backlogs\Default_2024.09.10_12.47.32-2024.09.10_15.34.53.txt");
            
            await ParseText(text);
        }

        public static async Task InitDictionaries()
        {
            _lookups = await JmDictHelper.LoadLookupTable();
            _allWords = (await JmDictHelper.LoadAllWords()).ToDictionary(word => word.WordId);
        }

        public static async Task<Deck> ParseText(string text)
        {
            if (!_initialized)
            {
                await _initSemaphore.WaitAsync();
                try
                {
                    if (!_initialized) // Double-check to avoid race conditions
                    {
                        await InitDictionaries();
                        _initialized = true;
                    }
                }
                finally
                {
                    _initSemaphore.Release();
                }
            }
            
            var timer = new Stopwatch();
            timer.Start();
            // var text = "見て";
            var parser = new Parser();
            var wordInfos = await parser.Parse(text);

            // TODO: support elongated vowels ふ～ -> ふう

            // Clean up special characters
            wordInfos.ForEach(x => x.Text = Regex.Replace(x.Text, "[^a-zA-Z0-9\u3040-\u309F\u30A0-\u30FF\u4E00-\u9FAF]", ""));

            // Remove empty lines
            wordInfos = wordInfos.Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList();
            
            // Filter bad lines that cause exceptions
            wordInfos.RemoveAll(w => w.Text == "ッー");

            Deconjugator deconjugator = new Deconjugator();

            List<(WordInfo wordInfo, int occurences)> uniqueWords = new();
            foreach (var word in wordInfos)
            {
                if (uniqueWords.Any(x => x.wordInfo.Text == word.Text)) continue;

                var occurences = wordInfos.Count(x => x.Text == word.Text);
                uniqueWords.Add((word, occurences));
            }

            // Create a thread-safe dictionary to store results with their original index
            var processedUniqueWords = new ConcurrentDictionary<int, DeckWord>();

            timer.Stop();
            double mecabTime = timer.Elapsed.TotalMilliseconds;

            timer.Restart();

            await Parallel.ForEachAsync(uniqueWords.Select((word, index) => new { word, index }), new CancellationToken(),
                                        async (item, _) =>
                                        {
                                            if (item.word.wordInfo.PartOfSpeech is PartOfSpeech.Verb or PartOfSpeech.IAdjective
                                                or PartOfSpeech.NaAdjective)
                                            {
                                                var deconjugated = deconjugator.Deconjugate(WanaKana.ToHiragana(item.word.wordInfo.Text))
                                                                               .Select(d => d.Text).ToList();
                                                deconjugated = deconjugated.OrderByDescending(d => d.Length).ToList();

                                                List<(string text, List<int> ids)> candidates = new();
                                                foreach (var form in deconjugated)
                                                {
                                                    if (_lookups.TryGetValue(form, out List<int> lookup))
                                                    {
                                                        candidates.Add((form, lookup));
                                                    }
                                                }

                                                if (candidates.Count == 0) return;

                                                foreach (var candidate in candidates)
                                                {
                                                    foreach (var id in candidate.ids)
                                                    {
                                                        if (!_allWords.TryGetValue(id, out var word)) continue;

                                                        List<PartOfSpeech> pos = word.PartsOfSpeech.Select(x => x.ToPartOfSpeech())
                                                                                     .ToList();
                                                        if (!pos.Contains(item.word.wordInfo.PartOfSpeech)) continue;

                                                        var normalizedReadings = word.Readings.Select(r => WanaKana.ToHiragana(r)).ToList();
                                                        var normalizedKanaReadings = word.KanaReadings.Select(r => WanaKana.ToHiragana(r))
                                                                                         .ToList();
                                                        byte readingType = normalizedReadings.Contains(candidate.text) ? (byte)0 : (byte)1;
                                                        byte readingIndex = readingType == 0
                                                            ? (byte)normalizedReadings.IndexOf(candidate.text)
                                                            : (byte)normalizedKanaReadings.IndexOf(candidate.text);

                                                        DeckWord deckWord = new()
                                                                            {
                                                                                WordId = id,
                                                                                ReadingType = readingType,
                                                                                ReadingIndex = readingIndex,
                                                                                Occurrences = item.word.occurences
                                                                            };
                                                        processedUniqueWords.TryAdd(item.index, deckWord);
                                                        break;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                var textInHiragana = WanaKana.ToHiragana(item.word.wordInfo.Text);

                                                if (_lookups.TryGetValue(textInHiragana, out List<int> candidates))
                                                {
                                                    // We take the first word for now, let's see if we can affine that later
                                                    var candidate = candidates[0];
                                                    if (!_allWords.TryGetValue(candidate, out var word)) return;

                                                    var normalizedReadings = word.Readings.Select(r => WanaKana.ToHiragana(r)).ToList();
                                                    var normalizedKanaReadings =
                                                        word.KanaReadings.Select(r => WanaKana.ToHiragana(r)).ToList();
                                                    byte readingType = normalizedReadings.Contains(textInHiragana) ? (byte)0 : (byte)1;
                                                    byte readingIndex = readingType == 0
                                                        ? (byte)normalizedReadings.IndexOf(textInHiragana)
                                                        : (byte)normalizedKanaReadings.IndexOf(textInHiragana);

                                                    DeckWord deckWord = new()
                                                                        {
                                                                            WordId = candidate,
                                                                            ReadingType = readingType,
                                                                            ReadingIndex = readingIndex,
                                                                            Occurrences = item.word.occurences
                                                                        };
                                                    processedUniqueWords.TryAdd(item.index, deckWord);
                                                }
                                            }
                                        });

            // Sort the results by their original index
            var orderedProcessedUniqueWords = processedUniqueWords
                                              .OrderBy(kvp => kvp.Key)
                                              .Select(kvp => kvp.Value)
                                              .ToList();

            // Assign the occurences for each word
            foreach (var deckWord in orderedProcessedUniqueWords)
            {
                // Remove one since we already counted it at the start
                deckWord.Occurrences--;
                deckWord.Occurrences +=
                    orderedProcessedUniqueWords.Count(x => x.WordId == deckWord.WordId && x.ReadingIndex == deckWord.ReadingIndex);
            }

            // deduplicate deconjugated words
            orderedProcessedUniqueWords = orderedProcessedUniqueWords
                                          .GroupBy(x => new { x.WordId, x.ReadingIndex })
                                          .Select(x => x.First())
                                          .ToList();
            timer.Stop();

            double deconjugationTime = timer.Elapsed.TotalMilliseconds;

            double totalTime = mecabTime + deconjugationTime;

            Console.WriteLine("Total words found : " + wordInfos.Count);

            // Console.WriteLine("Unique words found before deconjugation : " + uniqueWordInfos.Count);
            Console.WriteLine("Unique words found after deconjugation : " + orderedProcessedUniqueWords.Count);

            var characterCount = wordInfos.Sum(x => x.Text.Length);
            Console.WriteLine("Time elapsed: " + totalTime + "ms");
            Console.WriteLine($"Mecab time: {mecabTime:0.0}ms ({(mecabTime / totalTime * 100):0}%), Deconjugation time: {deconjugationTime:0.0}ms ({(deconjugationTime / totalTime * 100):0}%)");

            // Character count
            Console.WriteLine("Character count: " + characterCount);
            // Time for 10000 characters
            Console.WriteLine($"Time per 10000 characters: {(totalTime / characterCount * 10000):0.0}ms");
            // Time for 1million characters
            Console.WriteLine($"Time per 1 million characters: {(totalTime / characterCount * 1000000):0.0}ms");

            // write text to local file 1 by line "result"
            // await File.WriteAllLinesAsync(@"result.txt", wordInfos.Select(x => x.Text));
            // write results deconjugated
            // await File.WriteAllLinesAsync(@"deconjugated.txt", orderedProcessedUniqueWords);

            return new Deck
                   {
                       CharacterCount = characterCount,
                       WordCount = wordInfos.Count,
                       UniqueWordCount = orderedProcessedUniqueWords.Count,
                       UniqueWordUsedOnceCount = orderedProcessedUniqueWords.Count(x => x.Occurrences == 1),
                       UniqueKanjiCount = wordInfos.SelectMany(w => w.Text).Distinct().Count(c => WanaKana.IsKanji(c.ToString())),
                       UniqueKanjiUsedOnceCount = wordInfos.SelectMany(w => w.Text).GroupBy(c => c)
                                                           .Count(g => g.Count() == 1 && WanaKana.IsKanji(g.Key.ToString())),
                       //Difficulty = 
                       //AverageSentenceLength = 
                       DeckWords = orderedProcessedUniqueWords
                   };

            // ~250ms 7/10
        }
    }
}