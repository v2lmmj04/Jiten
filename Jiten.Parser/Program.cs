using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Jiten.Core;
using WanaKanaShaapu;

namespace Jiten.Parser
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            double mecabTime, deconjugationTime;

            var timer = new Stopwatch();
            timer.Start();
            var sentence =
                await File.ReadAllTextAsync(@"Y:\00_JapaneseStudy\JL\Backlogs\Default_2024.09.10_12.47.32-2024.09.10_15.34.53.txt");
            // var sentence = "見て";
            var parser = new Parser();
            var wordInfos = await parser.Parse(sentence);

            // TODO: support elongated vowels ふ～ -> ふう

            // Clean up special characters
            wordInfos.ForEach(x => x.Text = Regex.Replace(x.Text, "[^a-zA-Z0-9\u3040-\u309F\u30A0-\u30FF\u4E00-\u9FAF]", ""));

            // Remove empty lines
            wordInfos = wordInfos.Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList();

            Deconjugator deconjugator = new Deconjugator();

            // Deduplicate word infos for faster processing
            var uniqueWordInfos = wordInfos.GroupBy(x => x.Text).Select(x => x.First()).ToList();

            // Load all words into memory
            var lookups = await JMDictHelper.LoadLookupTable();
            var allWords = (await JMDictHelper.LoadAllWords()).ToDictionary(word => word.EntrySequenceId);

            // Create a thread-safe dictionary to store results with their original index
            var processedUniqueWords = new ConcurrentDictionary<int, string>();

            timer.Stop();
            mecabTime = timer.Elapsed.TotalMilliseconds;

            timer.Restart();

            await Parallel.ForEachAsync(uniqueWordInfos.Select((word, index) => new { word, index }), new CancellationToken(),
                                        async (item, _) =>
                                        {
                                            if (item.word.PartOfSpeech is PartOfSpeech.Verb or PartOfSpeech.IAdjective
                                                or PartOfSpeech.NaAdjective)
                                            {
                                                var deconjugated = deconjugator.Deconjugate(WanaKana.ToHiragana(item.word.Text))
                                                                               .Select(d => d.Text).ToList();
                                                deconjugated = deconjugated.OrderByDescending(d => d.Length).ToList();

                                                List<(string text, List<int> ids)> candidates = new();
                                                foreach (var form in deconjugated)
                                                {
                                                    if (lookups.TryGetValue(form, out List<int> lookup))
                                                    {
                                                        candidates.Add((form, lookup));
                                                    }
                                                }

                                                if (candidates.Count == 0) return;

                                                foreach (var candidate in candidates)
                                                {
                                                    foreach (var id in candidate.ids)
                                                    {
                                                        if (!allWords.TryGetValue(id, out var word)) continue;

                                                        List<PartOfSpeech> pos = word.PartsOfSpeech.Select(x => x.ToPartOfSpeech())
                                                                                     .ToList();
                                                        if (!pos.Contains(item.word.PartOfSpeech)) continue;

                                                        processedUniqueWords.TryAdd(item.index, candidate.text);
                                                        break;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                var textInHiragana = WanaKana.ToHiragana(item.word.Text);

                                                if (lookups.TryGetValue(textInHiragana, out List<int> _))
                                                {
                                                    processedUniqueWords.TryAdd(item.index, textInHiragana);
                                                }
                                            }
                                        });

            // Sort the results by their original index
            var orderedProcessedUniqueWords = processedUniqueWords.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToList();

            // deduplicate deconjugated words
            orderedProcessedUniqueWords = orderedProcessedUniqueWords.GroupBy(x => x).Select(x => x.First()).ToList();

            timer.Stop();

            deconjugationTime = timer.Elapsed.TotalMilliseconds;

            double totalTime = mecabTime + deconjugationTime;

            Console.WriteLine("Total words found : " + wordInfos.Count);

            Console.WriteLine("Unique words found before deconjugation : " + uniqueWordInfos.Count);
            Console.WriteLine("Unique words found after deconjugation : " + orderedProcessedUniqueWords.Count);

            var characterCount = sentence.Length;
            Console.WriteLine("Time elapsed: " + totalTime + "ms");
            Console.WriteLine($"Mecab time: {mecabTime:0.0}ms ({(mecabTime / totalTime * 100):0}%), Deconjugation time: {deconjugationTime:0.0}ms ({(deconjugationTime / totalTime * 100):0}%)");

            // Character count
            Console.WriteLine("Character count: " + characterCount);
            // Time for 10000 characters
            Console.WriteLine($"Time per 10000 characters: {(totalTime / characterCount * 10000):0.0}ms");
            // Time for 1million characters
            Console.WriteLine($"Time per 1 million characters: {(totalTime / characterCount * 1000000):0.0}ms");

            // write text to local file 1 by line "result"
            await File.WriteAllLinesAsync(@"result.txt", wordInfos.Select(x => x.Text));
            // write results deconjugated
            await File.WriteAllLinesAsync(@"deconjugated.txt", orderedProcessedUniqueWords);

            // ~250ms 7/10
        }
    }
}