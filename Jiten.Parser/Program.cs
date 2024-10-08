using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using MeCab;
using WanaKanaShaapu;

namespace Jiten.Parser
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var timer = new Stopwatch();
            timer.Start();
            var sentence = File.ReadAllText(@"Y:\00_JapaneseStudy\JL\Backlogs\Default_2024.09.10_12.47.32-2024.09.10_15.34.53.txt");
            var parser = new Parser();
            var wordInfos = await parser.Parse(sentence);
            
            // TODO: support elongated vowels ふ～ -> ふう

            // Clean up special characters
            wordInfos.ForEach(x => x.Text = Regex.Replace(x.Text, "[^a-zA-Z0-9\u3040-\u309F\u30A0-\u30FF\u4E00-\u9FAF]", ""));
            
            // Remove empty lines
            wordInfos = wordInfos.Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList();
            
            
            Deconjugator deconjugator = new Deconjugator();
            List<HashSet<DeconjugationForm>> deconjugations = new();
            foreach (var word in wordInfos)
            {
                //deconjugations.Add(deconjugator.Deconjugate(word.Text));
            }
            
            timer.Stop();

            Console.WriteLine("Total words found : " + wordInfos.Count);
            
            // unique wordinfos by text
            wordInfos = wordInfos.GroupBy(x => x.Text).Select(x => x.First()).ToList();
            Console.WriteLine("Unique words found : " + wordInfos.Count);
            
            var characterCount = sentence.Length;
            Console.WriteLine("Time elapsed: " + timer.Elapsed.TotalMilliseconds + "ms");
            // Character count
            Console.WriteLine("Character count: " + characterCount);
            // Time for 10000 characters
            Console.WriteLine("Time per 10000 characters: " + timer.Elapsed.TotalMilliseconds / characterCount * 10000 + "ms");
            // Time for 1million characters
            Console.WriteLine("Time per 1 million characters: " + timer.Elapsed.TotalMilliseconds / characterCount * 1000000 + "ms");
           
            // write text to local file 1 by line "result"
            await File.WriteAllLinesAsync(@"result.txt", wordInfos.Select(x => x.Text));
            // write results deconjugated
            await File.WriteAllLinesAsync(@"deconjugated.txt", deconjugations.SelectMany(x => x.Select(y => y.Text)));
            
            // ~250ms 7/10

        }

       
    }
}