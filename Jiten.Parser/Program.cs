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

        // Those cases are hardcoded, a better solution would be preferable but they don't work well with the current rules
        private static Dictionary<string, int> _specialCases = new()
                                                               {
                                                                   { "この", 1582920 },
                                                                   { "こと", 1313580 },
                                                                   { "もの", 1502390 },
                                                                   { "もん", 2780660 },
                                                                   { "くせ", 1509350 },
                                                                   { "いくら", 1219980 },
                                                                   { "とき", 1315840 },
                                                                   { "な", 2029110 },
                                                                   { "どん", 2142690 },
                                                                   { "いる", 1577980 },
                                                                   { "そう", 1006610 },
                                                                   { "ない", 1529520 },
                                                                   { "なんだ", 2119750 },
                                                                   { "わけ", 1538330 },
                                                                   { "いう", 1587040 },
                                                                   { "まま", 1585410 },
                                                                   { "いく", 1219950 },
                                                                   { "つく", 1495740 },
                                                                   { "たら", 2029050 },
                                                                   { "彼", 1483070 },
                                                               };

        public static async Task Main(string[] args)
        {
            // var text = await File.ReadAllTextAsync(@"Y:\00_JapaneseStudy\JL\Backlogs\Default_2024.09.10_12.47.32-2024.09.10_15.34.53.txt");
            var text = "風";

            await ParseTextToDeck(text);
        }

        public static async Task InitDictionaries()
        {
            _lookups = await JmDictHelper.LoadLookupTable();
            _allWords = (await JmDictHelper.LoadAllWords()).ToDictionary(word => word.WordId);
        }

        public static async Task<List<DeckWord>> ParseText(string text)
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

            var parser = new Parser();
            var wordInfos = await parser.Parse(text);

            // Only keep kanjis, kanas, digits,full width digits, latin characters, full width latin characters 
            wordInfos.ForEach(x => x.Text =
                                  Regex.Replace(x.Text,
                                                "[^a-zA-Z0-9\u3040-\u309F\u30A0-\u30FF\u4E00-\u9FAF\uFF21-\uFF3A\uFF41-\uFF5A\uFF10-\uFF19\u3005]",
                                                ""));
            // Remove empty lines
            wordInfos = wordInfos.Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList();

            // Filter bad lines that cause exceptions
            wordInfos.ForEach(x => x.Text = Regex.Replace(x.Text, "ッー", ""));

            Deconjugator deconjugator = new Deconjugator();

            var processWords = wordInfos.Select((word, index) => ProcessWord((word, 0), index, deconjugator)).ToList();

            var processedWords = await Task.WhenAll(processWords);
            return processedWords
                   .Where(result => result != null)
                   .Select(result => result!.Value.word)
                   .ToList();
        }

        public static async Task<Deck> ParseTextToDeck(string text)
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
            var parser = new Parser();
            var wordInfos = await parser.Parse(text);

            // TODO: support elongated vowels ふ～ -> ふう

            // Only keep kanjis, kanas, digits,full width digits, latin characters, full width latin characters 
            wordInfos.ForEach(x => x.Text =
                                  Regex.Replace(x.Text,
                                                "[^a-zA-Z0-9\u3040-\u309F\u30A0-\u30FF\u4E00-\u9FAF\uFF21-\uFF3A\uFF41-\uFF5A\uFF10-\uFF19\u3005]",
                                                ""));
            // Remove empty lines
            wordInfos = wordInfos.Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList();

            // Filter bad lines that cause exceptions
            // wordInfos.RemoveAll(w => w.Text is "ッー");
            wordInfos.ForEach(x => x.Text = Regex.Replace(x.Text, "ッー", ""));

            Deconjugator deconjugator = new Deconjugator();

            var uniqueWords = new List<(WordInfo wordInfo, int occurrences)>();
            var wordCount = new Dictionary<string, int>();

            foreach (var word in wordInfos)
            {
                if (!wordCount.TryAdd(word.Text, 1))
                    wordCount[word.Text]++;
                else
                    uniqueWords.Add((word, 1));
            }

            for (int i = 0; i < uniqueWords.Count; i++)
            {
                uniqueWords[i] = (uniqueWords[i].wordInfo, wordCount[uniqueWords[i].wordInfo.Text]);
            }

            // Create a thread-safe dictionary to store results with their original index
            var processedUniqueWords = new ConcurrentDictionary<int, DeckWord>();

            timer.Stop();
            double mecabTime = timer.Elapsed.TotalMilliseconds;

            timer.Restart();

            var processWords = uniqueWords.Select((word, index) => ProcessWord(word, index, deconjugator)).ToList();

            var processedWords = await Task.WhenAll(processWords);
            foreach (var result in processedWords)
            {
                if (result == null)
                    continue;

                processedUniqueWords.TryAdd(result.Value.index, result.Value.word);
            }

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

            // Split into sentences
            string[] sentences = Regex.Split(text, @"(?<=[。！？」）])|(?<=[…—])\r\n");
            sentences = sentences.Select(sentence =>
            {
                // Find the first Japanese character
                Match match = Regex.Match(sentence, @"[\p{IsHiragana}\p{IsKatakana}\p{IsCJKUnifiedIdeographs}]");
                if (match.Success)
                {
                    int startIndex = match.Index;
                    // Remove all special characters
                    return Regex.Replace(sentence.Substring(startIndex),
                                         "[^a-zA-Z0-9\u3040-\u309F\u30A0-\u30FF\u4E00-\u9FAF\uFF21-\uFF3A\uFF41-\uFF5A\uFF10-\uFF19\u3005]",
                                         "");
                }

                return "";
            }).Where(s => !string.IsNullOrEmpty(s)).ToArray();


            timer.Stop();

            double deconjugationTime = timer.Elapsed.TotalMilliseconds;

            double totalTime = mecabTime + deconjugationTime;

            Console.WriteLine("Total words found : " + wordInfos.Count);

            // Console.WriteLine("Unique words found before deconjugation : " + uniqueWordInfos.Count);
            Console.WriteLine("Unique words found after deconjugation : " + orderedProcessedUniqueWords.Count);

            var characterCount = wordInfos.Sum(x => x.Text.Length);
            Console.WriteLine($"Time elapsed: {totalTime:0.0}ms");
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
                       SentenceCount = sentences.Length,
                       DeckWords = orderedProcessedUniqueWords
                   };
        }

        private static async Task<(int index, DeckWord word)?> ProcessWord((WordInfo wordInfo, int occurrences) wordData, int index,
                                                                           Deconjugator deconjugator)
        {
            if (wordData.wordInfo.PartOfSpeech is PartOfSpeech.Verb or PartOfSpeech.IAdjective
                or PartOfSpeech.NaAdjective)
            {
                var deconjugated = deconjugator.Deconjugate(WanaKana.ToHiragana(wordData.wordInfo.Text))
                                               .Select(d => d.Text).ToList();
                deconjugated = deconjugated.OrderByDescending(d => d.Length).ToList();

                List<(string text, List<int> ids)> candidates = new();
                foreach (var form in deconjugated)
                {
                    if (_lookups.TryGetValue(form, out List<int> lookup))
                    {
                        // Reorder to have the most common words first. Might need a frequency list if it's not enough
                        var orderedLookup = lookup.OrderBy(l => l).ToList();
                        candidates.Add((form, orderedLookup));
                    }
                }

                if (candidates.Count == 0) return null;

                candidates = candidates.OrderByDescending(c => c.text.Length).ToList();

                // if there's a candidate that's the same as the base word, put it first in the list
                var baseDictionaryWord = WanaKana.ToHiragana(wordData.wordInfo.DictionaryForm);
                var baseDictionaryWordIndex = candidates.FindIndex(c => c.text == baseDictionaryWord);
                if (baseDictionaryWordIndex != -1)
                {
                    var baseDictionaryWordCandidate = candidates[baseDictionaryWordIndex];
                    candidates.RemoveAt(baseDictionaryWordIndex);
                    candidates.Insert(0, baseDictionaryWordCandidate);
                }

                // if there's a candidate that's the same as the base word, put it first in the list
                var baseWord = WanaKana.ToHiragana(wordData.wordInfo.Text);
                var baseWordIndex = candidates.FindIndex(c => c.text == baseWord);
                if (baseWordIndex != -1)
                {
                    var baseWordCandidate = candidates[baseWordIndex];
                    candidates.RemoveAt(baseWordIndex);
                    candidates.Insert(0, baseWordCandidate);
                }

                foreach (var candidate in candidates)
                {
                    foreach (var id in candidate.ids)
                    {
                        if (!_allWords.TryGetValue(id, out var word)) continue;

                        List<PartOfSpeech> pos = word.PartsOfSpeech.Select(x => x.ToPartOfSpeech())
                                                     .ToList();
                        if (!pos.Contains(wordData.wordInfo.PartOfSpeech)) continue;

                        var normalizedReadings = word.Readings.Select(r => WanaKana.ToHiragana(r)).ToList();
                        byte readingIndex = (byte)normalizedReadings.IndexOf(candidate.text);

                        DeckWord deckWord = new()
                                            {
                                                WordId = id,
                                                OriginalText = wordData.wordInfo.Text,
                                                ReadingIndex = readingIndex,
                                                Occurrences = wordData.occurrences
                                            };
                        return (index, deckWord);
                        break;
                    }
                }
            }
            else
            {
                var textInHiragana =
                    WanaKana.ToHiragana(wordData.wordInfo.Text,
                                        new DefaultOptions { ConvertLongVowelMark = false });

                if (_lookups.TryGetValue(textInHiragana, out List<int> candidates))
                {
                    candidates = candidates.OrderBy(c => c).ToList();

                    // Try to find the best match, use the firs tcandi
                    JmDictWord? bestMatch = null;

                    foreach (var id in candidates)
                    {
                        if (!_allWords.TryGetValue(id, out var word)) continue;

                        List<PartOfSpeech> pos = word.PartsOfSpeech.Select(x => x.ToPartOfSpeech())
                                                     .ToList();
                        if (!pos.Contains(wordData.wordInfo.PartOfSpeech)) continue;

                        bestMatch = word;
                        break;
                    }

                    if (bestMatch == null)
                    {
                        if (!_allWords.TryGetValue(candidates[0], out bestMatch)) return null;
                    }

                    if (_specialCases.TryGetValue(textInHiragana, out int specialCaseId))
                    {
                        bestMatch = _allWords[specialCaseId];
                    }

                    var normalizedReadings =
                        bestMatch.Readings.Select(r => WanaKana.ToHiragana(r)).ToList();
                    byte readingIndex = (byte)normalizedReadings.IndexOf(textInHiragana);

                    DeckWord deckWord = new()
                                        {
                                            WordId = bestMatch.WordId,
                                            OriginalText = wordData.wordInfo.Text,
                                            ReadingIndex = readingIndex,
                                            Occurrences = wordData.occurrences
                                        };
                    return (index, deckWord);
                }
            }

            return null;
        }
    }
}