using System.Text;
using System.Text.RegularExpressions;
using Jiten.Core.Data;
using Microsoft.Extensions.Configuration;

namespace Jiten.Parser;

public class MorphologicalAnalyser
{
    private static HashSet<(string, string, string, PartOfSpeech?)> SpecialCases3 =
    [
        ("な", "の", "で", PartOfSpeech.Expression),
        ("で", "は", "ない", PartOfSpeech.Expression),
        ("それ", "で", "も", PartOfSpeech.Conjunction),
        ("なく", "なっ", "た", PartOfSpeech.Verb)
    ];

    private static HashSet<(string, string, PartOfSpeech?)> SpecialCases2 =
    [
        ("じゃ", "ない", PartOfSpeech.Expression),
        ("に", "しろ", PartOfSpeech.Expression),
        ("だ", "けど", PartOfSpeech.Conjunction),
        ("だ", "が", PartOfSpeech.Conjunction),
        ("で", "さえ", PartOfSpeech.Expression),
        ("で", "すら", PartOfSpeech.Expression),
        ("と", "いう", PartOfSpeech.Expression),
        ("と", "か", PartOfSpeech.Conjunction),
        ("だ", "から", PartOfSpeech.Conjunction),
        ("これ", "まで", PartOfSpeech.Expression),
        ("それ", "も", PartOfSpeech.Conjunction),
        ("それ", "だけ", PartOfSpeech.Noun),
        ("くせ", "に", PartOfSpeech.Conjunction),
        ("の", "で", PartOfSpeech.Particle),
        ("誰", "も", PartOfSpeech.Expression),
        ("誰", "か", PartOfSpeech.Expression),
        ("すぐ", "に", PartOfSpeech.Adverb),
        ("なん", "か", PartOfSpeech.Particle),
        ("だっ", "た", PartOfSpeech.Expression),
        ("だっ", "たら", PartOfSpeech.Conjunction),
        ("よう", "に", PartOfSpeech.Expression),
        ("ん", "です", PartOfSpeech.Expression),
        ("ん", "だ", PartOfSpeech.Expression),
        ("です", "か", PartOfSpeech.Expression),
    ];

    private static readonly List<string> HonorificsSuffixes = ["さん", "ちゃん", "くん"];

    private readonly HashSet<char> _sentenceEnders = ['。', '！', '？', '」'];

    public async Task<List<SentenceInfo>> Parse(string text, bool morphemesOnly = false)
    {
        var configuration = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "..", "Shared", "sharedsettings.json"), optional: true)
                            .AddJsonFile("sharedsettings.json", optional: true)
                            .AddJsonFile("appsettings.json", optional: true)
                            .AddEnvironmentVariables()
                            .Build();

        // Build dictionary  sudachi ubuild Y:\CODE\Jiten\Shared\resources\user_dic.xml -s S:\Jiten\sudachi.rs\resources\system_full.dic -o "Y:\CODE\Jiten\Shared\resources\user_dic.dic"

        // Preprocess the text to remove invalid characters
        PreprocessText(ref text);

        // Custom stuff in the user dictionary interferes with the mode A morpheme parsing
        var configPath =
            morphemesOnly
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "sudachi_nouserdic.json")
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "sudachi.json");
        var dic = configuration.GetValue<string>("DictionaryPath");

        var allLines = text.Split("\n");

        var output = SudachiInterop.ProcessText(configPath, text, dic, mode: morphemesOnly ? 'A' : 'C').Split("\n");

        List<WordInfo> wordInfos = new();

        foreach (var line in output)
        {
            if (line == "EOS") continue;

            var wi = new WordInfo(line);
            if (!wi.IsInvalid)
                wordInfos.Add(wi);
        }

        if (morphemesOnly)
            return [new SentenceInfo("") { Words = wordInfos.Select(w => (w, (byte)0, (byte)0)).ToList() }];

        wordInfos = ProcessSpecialCases(wordInfos);
        wordInfos = CombineConjunctiveParticle(wordInfos);
        wordInfos = CombinePrefixes(wordInfos);

        wordInfos = CombineAmounts(wordInfos);
        wordInfos = CombineTte(wordInfos);
        wordInfos = CombineAuxiliaryVerbStem(wordInfos);
        wordInfos = CombineVerbDependant(wordInfos);
        wordInfos = CombineAdverbialParticle(wordInfos);
        wordInfos = CombineSuffix(wordInfos);
        wordInfos = CombineAuxiliary(wordInfos);
        wordInfos = CombineParticles(wordInfos);

        wordInfos = CombineFinal(wordInfos);

        wordInfos = SeparateSuffixHonorifics(wordInfos);

        return SplitIntoSentences(text, wordInfos);
    }

    private void PreprocessText(ref string text)
    {
        text = text.Replace("<", " ");
        text = text.Replace(">", " ");
        text = Regex.Replace(text,
                             "[^\u3040-\u309F\u30A0-\u30FF\u4E00-\u9FAF\uFF21-\uFF3A\uFF41-\uFF5A\uFF10-\uFF19\u3005\u3001-\u3003\u3008-\u3011\u3014-\u301F\uFF01-\uFF0F\uFF1A-\uFF1F\uFF3B-\uFF3F\uFF5B-\uFF60\uFF62-\uFF65．\\n…\u3000―\u2500()。！？「」）]",
                             "");

        // Force spaces and line breaks with some characters so sudachi doesn't try to include them as part of a word
        text = Regex.Replace(text, "「", "\n「 ");
        text = Regex.Replace(text, "」", " 」\n");
        text = Regex.Replace(text, "〈", " \n〈 ");
        text = Regex.Replace(text, "〉", " 〉\n");
        text = Regex.Replace(text, "《", " \n《 ");
        text = Regex.Replace(text, "》", " 》\n");
        text = Regex.Replace(text, "“", " \n“ ");
        text = Regex.Replace(text, "”", " ”\n");
        text = Regex.Replace(text, "―", " ― ");
        text = Regex.Replace(text, "。", " 。\n");
        text = Regex.Replace(text, "！", " ！\n");
        text = Regex.Replace(text, "？", " ？\n");
    }

    /// <summary>
    /// Handle special cases that could not be covered by the other rules
    /// </summary>
    /// <param name="wordInfos"></param>
    /// <returns></returns>
    private List<WordInfo> ProcessSpecialCases(List<WordInfo> wordInfos)
    {
        if (wordInfos.Count == 0)
            return wordInfos;

        List<WordInfo> newList = new List<WordInfo>(wordInfos.Count);


        for (int i = 0; i < wordInfos.Count;)
        {
            WordInfo w1 = wordInfos[i];

            if (i < wordInfos.Count - 2)
            {
                WordInfo w2 = wordInfos[i + 1];
                WordInfo w3 = wordInfos[i + 2];

                // surukudasai
                if (w1.DictionaryForm == "する" && w2.Text == "て" && w3.DictionaryForm == "くださる")
                {
                    var newWord = new WordInfo(w1);
                    newWord.Text = w1.Text + w2.Text + w3.Text;

                    newList.Add(newWord);
                    i += 3;

                    continue;
                }

                bool found = false;
                foreach (var sc in SpecialCases3)
                {
                    if (w1.Text == sc.Item1 && w2.Text == sc.Item2 && w3.Text == sc.Item3)
                    {
                        var newWord = new WordInfo(w1);
                        newWord.Text = w1.Text + w2.Text + w3.Text;

                        if (sc.Item4 != null)
                        {
                            newWord.PartOfSpeech = sc.Item4.Value;
                        }

                        newList.Add(newWord);
                        i += 3;
                        found = true;
                        break;
                    }
                }

                if (found)
                    continue;
            }

            if (i < wordInfos.Count - 1)
            {
                WordInfo w2 = wordInfos[i + 1];

                bool found = false;
                foreach (var sc in SpecialCases2)
                {
                    if (w1.Text == sc.Item1 && w2.Text == sc.Item2)
                    {
                        var newWord = new WordInfo(w1);
                        newWord.Text = w1.Text + w2.Text;

                        if (sc.Item3 != null)
                        {
                            newWord.PartOfSpeech = sc.Item3.Value;
                        }

                        newList.Add(newWord);
                        i += 2;
                        found = true;
                        break;
                    }
                }

                if (found)
                    continue;
            }

            // This word is (sometimes?) parsed as auxiliary for some reason
            if (w1.Text == "でしょう")
            {
                var newWord = new WordInfo(w1);
                newWord.PartOfSpeech = PartOfSpeech.Expression;
                newWord.PartOfSpeechSection1 = PartOfSpeechSection.None;

                newList.Add(newWord);
                i++;
                continue;
            }

            if (w1.Text == "絶対無理")
            {
                var zettai = new WordInfo
                             {
                                 Text = "絶対", DictionaryForm = "絶対", PartOfSpeech = PartOfSpeech.Adverb,
                                 PartOfSpeechSection1 = PartOfSpeechSection.None, Reading = "ぜったい"
                             };
                var muri = new WordInfo
                           {
                               Text = "無理", DictionaryForm = "無理", PartOfSpeech = PartOfSpeech.NaAdjective,
                               PartOfSpeechSection1 = PartOfSpeechSection.None, Reading = "むり"
                           };

                newList.Add(zettai);
                newList.Add(muri);
                i++;
                continue;
            }

            newList.Add(w1);
            i++;
        }

        return newList;
    }

    private List<WordInfo> CombinePrefixes(List<WordInfo> wordInfos)
    {
        if (wordInfos.Count < 2)
            return wordInfos;

        List<WordInfo> newList = new List<WordInfo>(wordInfos.Count);
        var currentWord = new WordInfo(wordInfos[0]);

        for (int i = 1; i < wordInfos.Count; i++)
        {
            var nextWord = wordInfos[i];
            if (currentWord.PartOfSpeech == PartOfSpeech.Prefix)
            {
                var newText = currentWord.Text + nextWord.Text;
                currentWord = new WordInfo(nextWord);
                currentWord.Text = newText;
            }
            else
            {
                newList.Add(currentWord);
                currentWord = new WordInfo(nextWord);
            }
        }

        newList.Add(currentWord);

        return newList;
    }

    private List<WordInfo> CombineAmounts(List<WordInfo> wordInfos)
    {
        if (wordInfos.Count < 2)
            return wordInfos;

        List<WordInfo> newList = new List<WordInfo>(wordInfos.Count);
        var currentWord = new WordInfo(wordInfos[0]);
        for (int i = 1; i < wordInfos.Count; i++)
        {
            var nextWord = wordInfos[i];

            if ((currentWord.HasPartOfSpeechSection(PartOfSpeechSection.Amount) ||
                 currentWord.HasPartOfSpeechSection(PartOfSpeechSection.Numeral)) &&
                AmountCombinations.Combinations.Contains((currentWord.Text, nextWord.Text)))
            {
                var text = currentWord.Text + nextWord.Text;
                currentWord = new WordInfo(nextWord);
                currentWord.Text = text;
                currentWord.PartOfSpeech = PartOfSpeech.Noun;
            }
            else
            {
                newList.Add(currentWord);
                currentWord = new WordInfo(nextWord);
            }
        }

        newList.Add(currentWord);

        return newList;
    }

    private List<WordInfo> CombineTte(List<WordInfo> wordInfos)
    {
        if (wordInfos.Count < 2)
            return wordInfos;

        List<WordInfo> newList = new List<WordInfo>(wordInfos.Count);
        var currentWord = new WordInfo(wordInfos[0]);
        for (int i = 1; i < wordInfos.Count; i++)
        {
            WordInfo nextWord = wordInfos[i];

            if (currentWord.Text.EndsWith("っ") && nextWord.Text.StartsWith("て"))
            {
                currentWord.Text += nextWord.Text;
            }
            else
            {
                newList.Add(currentWord);
                currentWord = new WordInfo(nextWord);
            }
        }

        newList.Add(currentWord);

        return newList;
    }

    private List<WordInfo> CombineVerbDependant(List<WordInfo> wordInfos)
    {
        if (wordInfos.Count < 2)
            return wordInfos;

        wordInfos = CombineVerbDependants(wordInfos);
        wordInfos = CombineVerbPossibleDependants(wordInfos);
        wordInfos = CombineVerbDependantsSuru(wordInfos);
        wordInfos = CombineVerbDependantsTeiru(wordInfos);

        return wordInfos;
    }

    private List<WordInfo> CombineVerbDependants(List<WordInfo> wordInfos)
    {
        if (wordInfos.Count < 2)
            return wordInfos;

        List<WordInfo> newList = new List<WordInfo>();
        WordInfo currentWord = new WordInfo(wordInfos[0]);

        for (int i = 1; i < wordInfos.Count; i++)
        {
            WordInfo nextWord = wordInfos[i];

            if (nextWord.HasPartOfSpeechSection(PartOfSpeechSection.Dependant) &&
                currentWord.PartOfSpeech == PartOfSpeech.Verb)
            {
                currentWord.Text += nextWord.Text;
            }
            else
            {
                newList.Add(currentWord);
                currentWord = new WordInfo(nextWord);
            }
        }

        newList.Add(currentWord);
        return newList;
    }

    private List<WordInfo> CombineVerbPossibleDependants(List<WordInfo> wordInfos)
    {
        if (wordInfos.Count < 2)
            return wordInfos;

        List<WordInfo> newList = new List<WordInfo>();
        WordInfo currentWord = new WordInfo(wordInfos[0]);

        for (int i = 1; i < wordInfos.Count; i++)
        {
            WordInfo nextWord = wordInfos[i];

            // Condition uses accumulator (verb) and next word (possible dependant + specific forms)
            if (nextWord.HasPartOfSpeechSection(PartOfSpeechSection.PossibleDependant) &&
                currentWord.PartOfSpeech == PartOfSpeech.Verb &&
                (nextWord.DictionaryForm == "得る" ||
                 nextWord.DictionaryForm == "する" ||
                 nextWord.DictionaryForm == "しまう" ||
                 nextWord.DictionaryForm == "おる" ||
                 nextWord.DictionaryForm == "きる" ||
                 nextWord.DictionaryForm == "こなす" ||
                 nextWord.DictionaryForm == "いく" ||
                 nextWord.DictionaryForm == "貰う" ||
                 nextWord.DictionaryForm == "いる"
                ))
            {
                currentWord.Text += nextWord.Text;
            }
            else
            {
                newList.Add(currentWord);
                currentWord = new WordInfo(nextWord);
            }
        }

        newList.Add(currentWord);
        return newList;
    }

    private List<WordInfo> CombineVerbDependantsSuru(List<WordInfo> wordInfos)
    {
        if (wordInfos.Count < 2)
            return wordInfos;

        List<WordInfo> newList = new List<WordInfo>();
        int i = 0;
        while (i < wordInfos.Count)
        {
            WordInfo currentWord = wordInfos[i];

            if (i + 1 < wordInfos.Count)
            {
                WordInfo nextWord = wordInfos[i + 1];
                if (currentWord.HasPartOfSpeechSection(PartOfSpeechSection.PossibleSuru) &&
                    nextWord.DictionaryForm == "する" && nextWord.Text != "する" && nextWord.Text != "しない")
                {
                    WordInfo combinedWord = new WordInfo(currentWord);
                    combinedWord.Text += nextWord.Text;
                    combinedWord.PartOfSpeech = PartOfSpeech.Verb;
                    newList.Add(combinedWord);
                    i += 2;
                    continue;
                }
            }

            newList.Add(new WordInfo(currentWord));
            i++;
        }

        return newList;
    }

    private List<WordInfo> CombineVerbDependantsTeiru(List<WordInfo> wordInfos)
    {
        if (wordInfos.Count < 3)
            return wordInfos;

        List<WordInfo> newList = new List<WordInfo>();
        int i = 0;
        while (i < wordInfos.Count)
        {
            WordInfo currentWord = wordInfos[i];

            if (i + 2 < wordInfos.Count)
            {
                WordInfo nextWord1 = wordInfos[i + 1];
                WordInfo nextWord2 = wordInfos[i + 2];

                if (currentWord.PartOfSpeech == PartOfSpeech.Verb &&
                    nextWord1.DictionaryForm == "て" &&
                    nextWord2.DictionaryForm == "いる")
                {
                    WordInfo combinedWord = new WordInfo(currentWord);
                    combinedWord.Text += nextWord1.Text + nextWord2.Text;
                    newList.Add(combinedWord);
                    i += 3;
                    continue;
                }
            }

            newList.Add(new WordInfo(currentWord));
            i++;
        }

        return newList;
    }

    private List<WordInfo> CombineAdverbialParticle(List<WordInfo> wordInfos)
    {
        if (wordInfos.Count < 2)
            return wordInfos;

        List<WordInfo> newList = new List<WordInfo>();
        WordInfo currentWord = new WordInfo(wordInfos[0]);

        for (int i = 1; i < wordInfos.Count; i++)
        {
            WordInfo nextWord = wordInfos[i];

            // i.e　だり, たり
            if (nextWord.HasPartOfSpeechSection(PartOfSpeechSection.AdverbialParticle) &&
                (nextWord.DictionaryForm == "だり" || nextWord.DictionaryForm == "たり") &&
                currentWord.PartOfSpeech == PartOfSpeech.Verb)

            {
                currentWord.Text += nextWord.Text;
            }
            else
            {
                newList.Add(currentWord);
                currentWord = new WordInfo(nextWord);
            }
        }

        newList.Add(currentWord);

        return newList;
    }

    private List<WordInfo> CombineConjunctiveParticle(List<WordInfo> wordInfos)
    {
        if (wordInfos.Count < 2)
            return wordInfos;

        List<WordInfo> newList = [wordInfos[0]];

        for (int i = 1; i < wordInfos.Count; i++)
        {
            WordInfo currentWord = wordInfos[i];
            WordInfo previousWord = newList[^1];
            bool combined = false;

            if (currentWord.HasPartOfSpeechSection(PartOfSpeechSection.ConjunctionParticle) &&
                currentWord.Text is "て" or "で" or "ちゃ" or "ば" &&
                previousWord.PartOfSpeech == PartOfSpeech.Verb)
            {
                previousWord.Text += currentWord.Text;
                combined = true;
            }

            if (!combined)
            {
                newList.Add(currentWord);
            }
        }

        return newList;
    }

    private List<WordInfo> CombineAuxiliary(List<WordInfo> wordInfos)
    {
        if (wordInfos.Count < 2)
            return wordInfos;

        List<WordInfo> newList =
        [
            wordInfos[0]
        ];

        for (int i = 1; i < wordInfos.Count; i++)
        {
            WordInfo currentWord = wordInfos[i];
            WordInfo previousWord = newList[^1];
            bool combined = false;

            if (currentWord.PartOfSpeech != PartOfSpeech.Auxiliary)
            {
                newList.Add(currentWord);
                continue;
            }

            if (previousWord.PartOfSpeech is PartOfSpeech.Verb or PartOfSpeech.IAdjective
                && currentWord.Text != "な"
                && currentWord.Text != "に"
                && currentWord.DictionaryForm != "です"
                && currentWord.DictionaryForm != "らしい"
                && currentWord.Text != "なら"
                && currentWord.DictionaryForm != "べし"
                && currentWord.DictionaryForm != "ようだ"
                && currentWord.Text != "だろう"
               )
            {
                previousWord.Text += currentWord.Text;
                combined = true;
            }

            if (currentWord.Text == "な" &&
                (previousWord.HasPartOfSpeechSection(PartOfSpeechSection.PossibleNaAdjective) ||
                 previousWord.HasPartOfSpeechSection(PartOfSpeechSection.NaAdjectiveLike) ||
                 previousWord.PartOfSpeech == PartOfSpeech.NaAdjective))
            {
                previousWord.Text += currentWord.Text;
                previousWord.PartOfSpeech = PartOfSpeech.NaAdjective;
                combined = true;
            }

            if (!combined)
            {
                newList.Add(currentWord);
            }
        }

        return newList;
    }

    private List<WordInfo> CombineAuxiliaryVerbStem(List<WordInfo> wordInfos)
    {
        if (wordInfos.Count < 2)
            return wordInfos;

        List<WordInfo> newList = new List<WordInfo>();
        WordInfo currentWord = new WordInfo(wordInfos[0]);

        for (int i = 1; i < wordInfos.Count; i++)
        {
            var nextWord = wordInfos[i];

            if (wordInfos[i].HasPartOfSpeechSection(PartOfSpeechSection.AuxiliaryVerbStem) &&
                wordInfos[i].Text != "ように" &&
                wordInfos[i].Text != "よう" &&
                wordInfos[i].Text != "みたい" &&
                (wordInfos[i - 1].PartOfSpeech == PartOfSpeech.Verb || wordInfos[i - 1].PartOfSpeech == PartOfSpeech.IAdjective))
            {
                currentWord.Text += nextWord.Text;
            }
            else
            {
                newList.Add(currentWord);
                currentWord = new WordInfo(nextWord);
            }
        }

        newList.Add(currentWord);

        return newList;
    }

    private List<WordInfo> CombineSuffix(List<WordInfo> wordInfos)
    {
        if (wordInfos.Count < 2)
            return wordInfos;

        List<WordInfo> newList = new List<WordInfo>();
        WordInfo currentWord = new WordInfo(wordInfos[0]);

        for (int i = 1; i < wordInfos.Count; i++)
        {
            var nextWord = wordInfos[i];

            if ((wordInfos[i].PartOfSpeech == PartOfSpeech.Suffix || wordInfos[i].HasPartOfSpeechSection(PartOfSpeechSection.Suffix))
                && (wordInfos[i].DictionaryForm == "っこ"
                    || wordInfos[i].DictionaryForm == "さ"
                    || wordInfos[i].DictionaryForm == "がる"
                    || ((wordInfos[i].DictionaryForm == "たち" || wordInfos[i].DictionaryForm == "ら") &&
                        wordInfos[i - 1].PartOfSpeech == PartOfSpeech.Pronoun)))
            {
                currentWord.Text += nextWord.Text;
            }
            else
            {
                newList.Add(currentWord);
                currentWord = new WordInfo(nextWord);
            }
        }

        newList.Add(currentWord);
        return newList;
    }

    private List<WordInfo> CombineParticles(List<WordInfo> wordInfos)
    {
        if (wordInfos.Count < 2)
            return wordInfos;

        List<WordInfo> newList = new List<WordInfo>();
        int i = 0;
        while (i < wordInfos.Count)
        {
            WordInfo currentWord = wordInfos[i];

            if (i + 1 < wordInfos.Count)
            {
                WordInfo nextWord = wordInfos[i + 1];
                string combinedText = "";

                if (currentWord.Text == "に" && nextWord.Text == "は") combinedText = "には";
                else if (currentWord.Text == "と" && nextWord.Text == "は") combinedText = "とは";
                else if (currentWord.Text == "で" && nextWord.Text == "は") combinedText = "では";
                else if (currentWord.Text == "の" && nextWord.Text == "に") combinedText = "のに";

                if (!string.IsNullOrEmpty(combinedText))
                {
                    WordInfo combinedWord = new WordInfo(currentWord);
                    combinedWord.Text = combinedText;
                    newList.Add(combinedWord);
                    i += 2;
                    continue;
                }
            }

            newList.Add(new WordInfo(currentWord));
            i++;
        }

        return newList;
    }

    /// <summary>
    /// Tries to separate the honorifics from the proper names
    /// This still doesn't work for all cases
    /// </summary>
    /// <param name="wordInfos"></param>
    /// <returns></returns>
    private List<WordInfo> SeparateSuffixHonorifics(List<WordInfo> wordInfos)
    {
        if (wordInfos.Count < 2)
            return wordInfos;

        List<WordInfo> newList = new List<WordInfo>();

        for (var i = 0; i < wordInfos.Count; i++)
        {
            WordInfo currentWord = new WordInfo(wordInfos[i]);
            bool separated = false;
            foreach (var honorific in HonorificsSuffixes)
            {
                if (!currentWord.Text.EndsWith(honorific) || currentWord.Text.Length <= honorific.Length ||
                    (!currentWord.HasPartOfSpeechSection(PartOfSpeechSection.PersonName) &&
                     !currentWord.HasPartOfSpeechSection(PartOfSpeechSection.ProperNoun))) continue;

                currentWord.Text = currentWord.Text.Substring(0, currentWord.Text.Length - honorific.Length);
                if (currentWord.DictionaryForm.EndsWith(honorific))
                {
                    currentWord.DictionaryForm =
                        currentWord.DictionaryForm.Substring(0, currentWord.DictionaryForm.Length - honorific.Length);
                }

                var suffix = new WordInfo()
                             {
                                 Text = honorific, PartOfSpeech = PartOfSpeech.Suffix, Reading = honorific, DictionaryForm = honorific
                             };
                newList.Add(currentWord);
                newList.Add(suffix);
                separated = true;

                break;
            }

            if (!separated)
                newList.Add(currentWord);
        }

        return wordInfos;
    }

    /// <summary>
    /// Cleanup method / 2nd pass for some cases
    /// </summary>
    /// <param name="wordInfos"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private List<WordInfo> CombineFinal(List<WordInfo> wordInfos)
    {
        if (wordInfos.Count < 2)
            return wordInfos;

        List<WordInfo> newList = new List<WordInfo>();
        WordInfo currentWord = new WordInfo(wordInfos[0]);

        for (int i = 1; i < wordInfos.Count; i++)
        {
            var nextWord = wordInfos[i];

            if (wordInfos[i].Text == "ば" &&
                wordInfos[i - 1].PartOfSpeech == PartOfSpeech.Verb)
            {
                currentWord.Text += nextWord.Text;
            }
            else
            {
                newList.Add(currentWord);
                currentWord = new WordInfo(nextWord);
            }
        }

        newList.Add(currentWord);

        return newList;
    }

    private List<SentenceInfo> SplitIntoSentences(string text, List<WordInfo> wordInfos)
    {
        var sentences = new List<SentenceInfo>();

        var sb = new StringBuilder();
        bool seenEnder = false;

        // Need a flat text for the sentence to corresponds if they're cut between 2 lines
        text = text.Replace("\r", "").Replace("\n", "");

        for (int i = 0; i < text.Length; i++)
        {
            char current = text[i];
            sb.Append(current);

            // Detect if sentence ender was seen
            if (_sentenceEnders.Contains(current))
            {
                seenEnder = true;
                continue;
            }

            // Flush the sentence if there's an ellipsis at the end of a line
            if (current == '\n' && text[i - 1] == '…')
            {
                sentences.Add(new SentenceInfo(sb.ToString()));
                sb.Clear();
                seenEnder = false;
                continue;
            }

            // Handle possible multiple enders in a row
            if (seenEnder)
            {
                if (_sentenceEnders.Contains(current))
                    continue;

                // Flush the sentence and append the character to the next one instead
                var lastCharacter = sb[^1];
                sentences.Add(new SentenceInfo(sb.ToString(0, sb.Length - 1)));
                sb.Clear();
                sb.Append(lastCharacter);
                seenEnder = false;
            }
        }

        // Handle leftover buffer
        if (sb.Length > 0)
        {
            sentences.Add(new SentenceInfo(sb.ToString()));
        }

        int currentSentenceIndex = 0;
        int currentCharIndex = 0;
        foreach (var word in wordInfos)
        {
            if (string.IsNullOrEmpty(word.Text))
                continue;

            bool wordAssigned = false;
            while (currentSentenceIndex < sentences.Count && !wordAssigned)
            {
                var sentence = sentences[currentSentenceIndex];
                int wordIndex = sentence.Text.IndexOf(word.Text, currentCharIndex, StringComparison.Ordinal);

                if (wordIndex >= 0)
                {
                    currentCharIndex = wordIndex + word.Text.Length;
                    sentences[currentSentenceIndex].Words.Add((word, (byte)wordIndex, (byte)word.Text.Length));
                    wordAssigned = true;
                }
                else
                {
                    // Word not found, check if it spans across to the next sentence
                    if (currentSentenceIndex + 1 < sentences.Count)
                    {
                        var remainingTextInCurrentSentence = currentCharIndex < sentence.Text.Length
                            ? sentence.Text[currentCharIndex..]
                            : string.Empty;
                        var nextSentence = sentences[currentSentenceIndex + 1];

                        for (int i = 1; i < word.Text.Length; i++)
                        {
                            string part1 = word.Text[..i];
                            string part2 = word.Text[i..];
                            if (!remainingTextInCurrentSentence.EndsWith(part1) || !nextSentence.Text.StartsWith(part2)) continue;

                            // Word spans sentences, merge them
                            sentence.Text += nextSentence.Text;
                            sentences.RemoveAt(currentSentenceIndex + 1);

                            // Retry finding the word in the merged sentence
                            wordIndex = sentence.Text.IndexOf(word.Text, currentCharIndex, StringComparison.Ordinal);
                            if (wordIndex >= 0)
                            {
                                currentCharIndex = wordIndex + word.Text.Length;
                                sentence.Words.Add((word, (byte)wordIndex, (byte)word.Text.Length));
                                wordAssigned = true;
                            }

                            break;
                        }
                    }

                    if (wordAssigned) continue;

                    currentSentenceIndex++;
                    currentCharIndex = 0;
                }
            }
        }

        return sentences;
    }
}