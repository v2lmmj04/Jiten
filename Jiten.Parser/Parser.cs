using System.Runtime.InteropServices;
using Jiten.Core.Data;

namespace Jiten.Parser;

class SudachiInterop
{
    // Import the `run_cli_ffi` function from the Rust DLL
    [DllImport(@"Y:\CODE\Forks\sudachi.rs\target\release\sudachi_lib.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr run_cli_ffi(string configPath, string filePath, string dictionaryPath, string outputPath);

    // Import the `free_string` function to free memory allocated in Rust
    [DllImport(@"Y:\CODE\Forks\sudachi.rs\target\release\sudachi_lib.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void free_string(IntPtr ptr);


    public static string RunCli(string configPath, string filePath, string dictionaryPath, string outputPath)
    {
        // Call the FFI function
        IntPtr resultPtr = run_cli_ffi(configPath, filePath, dictionaryPath, outputPath);

        // Convert the result to a C# string
        string result = Marshal.PtrToStringAnsi(resultPtr) ?? string.Empty;
        
        // Free the string allocated in Rust
        free_string(resultPtr);

        return result;
    }
}


public class Parser
{
    private static HashSet<(string, string, string)> SpecialCases3 = new HashSet<(string, string, string)> { ("な", "の", "で"),
                                                                         ( "で", "は" ,"ない")
                                                                     };
    
    private static HashSet<(string, string)> SpecialCases2 = new HashSet<(string, string)>
                                                            {
                                                                ("じゃ", "ない"),
                                                                ("に", "しろ"),
                                                                ("だ", "けど"),
                                                                ("だ", "が"),
                                                                ("で", "さえ"),
                                                                ("で", "すら"),
                                                                ("と", "いう"),
                                                                ("と", "か"),
                                                                ("だ", "から"),
                                                                ("これ", "まで"),
                                                                ("くせ", "に"),
                                                                ("の", "で"),
                                                                ("誰", "も"),
                                                                ("誰", "か"),
                                                                ("すぐ", "に"),
                                                                ("なん", "か")
                                                            };

    
    // public List<WordInfo> Parse(string text)
    // {
    //     var parameters = new MeCabParam() { DicDir = @"Y:\CODE\JapaneseParser\CustomDics\unidic", };
    //     var tagger = MeCabTagger.Create(parameters);
    //     var nodes = tagger.ParseToNodes(text).ToList();
    //     List<WordInfo> wordInfos = new List<WordInfo>();
    //
    //     foreach (var node in nodes)
    //     {
    //         if (node.CharType > 0)
    //             wordInfos.Add(new WordInfo(node));
    //     }
    //
    //     wordInfos = CombineConjunctiveParticle(wordInfos);
    //     wordInfos = CombinePrefixes(wordInfos);
    //     wordInfos = CombineAmounts(wordInfos);
    //     wordInfos = CombineVerbDependant(wordInfos);
    //     wordInfos = CombineAuxiliary(wordInfos);
    //     wordInfos = CombineAuxiliaryVerbStem(wordInfos);
    //     wordInfos = CombineSuffix(wordInfos);
    //
    //     return wordInfos;
    // }

    public async Task<List<WordInfo>> Parse(string text)
    {
        // Build dictionary  sudachi ubuild Y:\CODE\Jiten\Jiten.Parser\resources\user_dic.xml -s F:\00_RawJap\sudachi.rs\resources\system_full.dic -o "Y:\CODE\Jiten\Jiten.Parser\resources\user_dic.dic"
        
        // Preprocess the text to remove invalid characters
        PreprocessText(ref text);

        //TODO: find a better way than using a temp file
        string tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, text);

        var outputFile = Path.GetTempFileName();
        var configPath = @"Y:\CODE\Jiten\Jiten.Parser\resources\sudachi.json";
        var dic = @"F:\00_RawJap\sudachi.rs\resources\system_full.dic";

        var path = @$"F:\00_RawJap\sudachi.rs\target\release\deps\sudachi.exe";

        SudachiInterop.RunCli(configPath, tempFilePath, dic, outputFile);

        // var process = new Process();
        // process.StartInfo.FileName = path;
        // process.StartInfo.Arguments = $"--dict {dic} -a -o {outputFile} -r {configPath}  {tempFilePath}";
        //
        // process.Start();
        //
        // await process.WaitForExitAsync();

        var output = (await File.ReadAllLinesAsync(outputFile)).ToList();
        List<WordInfo> wordInfos = new();

        foreach (var line in output)
        {
            if (line == "EOS") continue;

            var wi = new WordInfo(line);
            if (!wi.IsInvalid)
                wordInfos.Add(wi);
        }

        wordInfos = ProcessSpecialCases(wordInfos);
        wordInfos = CombineConjunctiveParticle(wordInfos);
        wordInfos = CombinePrefixes(wordInfos);
        wordInfos = CombineAmounts(wordInfos);
        wordInfos = CombineTte(wordInfos);
        wordInfos = CombineAuxiliary(wordInfos);
        wordInfos = CombineAuxiliaryVerbStem(wordInfos);
        wordInfos = CombineVerbDependant(wordInfos);
        wordInfos = CombineAdverbialParticle(wordInfos);
        wordInfos = CombineSuffix(wordInfos);
        wordInfos = CombineParticles(wordInfos);

        wordInfos = CombineFinal(wordInfos);
        
        return wordInfos;
    }

    private void PreprocessText(ref string text)
    {
        text = text.Replace("<", " ");
        text = text.Replace(">", " ");
    }

    /// <summary>
    /// Handle special cases that could not be covered by the other rules
    /// </summary>
    /// <param name="wordInfos"></param>
    /// <returns></returns>
    private List<WordInfo> ProcessSpecialCases(List<WordInfo> wordInfos)
    {
        for (int i = 0; i < wordInfos.Count - 2; i++)
        {
            // surukudasai
            if (wordInfos[i].DictionaryForm == "する" && wordInfos[i + 1].Text == "て" && wordInfos[i + 2].DictionaryForm == "くださる")
            {
                wordInfos[i].Text = wordInfos[i].Text + wordInfos[i + 1].Text + wordInfos[i + 2].Text;
                wordInfos.RemoveAt(i + 1);
                wordInfos.RemoveAt(i + 1);
            }

            foreach (var sc in SpecialCases3)
            {
                if (wordInfos[i].Text == sc.Item1 && wordInfos[i + 1].Text == sc.Item2 && wordInfos[i + 2].Text == sc.Item3)
                {
                    wordInfos[i].Text = wordInfos[i].Text + wordInfos[i + 1].Text + wordInfos[i + 2].Text;
                    wordInfos.RemoveAt(i + 1);
                    wordInfos.RemoveAt(i + 1);
                }
            }
        }

        for (int i = 0; i < wordInfos.Count - 1; i++)
        {
            foreach (var sc in SpecialCases2)
            {
                if (wordInfos[i].Text == sc.Item1 && wordInfos[i + 1].Text == sc.Item2)
                {
                    wordInfos[i].Text += wordInfos[i + 1].Text;
                    wordInfos.RemoveAt(i + 1);
                }
            }
        }

        return wordInfos;
    }

    private List<WordInfo> CombinePrefixes(List<WordInfo> wordInfos)
    {
        for (int i = 0; i < wordInfos.Count - 1; i++)
        {
            if (wordInfos[i].PartOfSpeech == PartOfSpeech.Prefix)
            {
                wordInfos[i + 1].Text = wordInfos[i].Text + wordInfos[i + 1].Text;
                wordInfos.RemoveAt(i);
                i--;
            }
        }

        return wordInfos;
    }

    private List<WordInfo> CombineAmounts(List<WordInfo> wordInfos)
    {
        for (int i = 0; i < wordInfos.Count - 1; i++)
        {
            if (wordInfos[i].HasPartOfSpeechSection(PartOfSpeechSection.Amount) ||
                wordInfos[i].HasPartOfSpeechSection(PartOfSpeechSection.Numeral))
            {
                wordInfos[i + 1].Text = wordInfos[i].Text + wordInfos[i + 1].Text;
                wordInfos[i + 1].PartOfSpeech = PartOfSpeech.Noun;
                wordInfos.RemoveAt(i);
                i--;
            }
        }

        return wordInfos;
    }

    private List<WordInfo> CombineTte(List<WordInfo> wordInfos)
    {
        for (int i = 0; i < wordInfos.Count - 1; i++)
        {
            if (wordInfos[i].Text.EndsWith("っ") && wordInfos[i + 1].Text.StartsWith("て"))
            {
                wordInfos[i].Text += wordInfos[i + 1].Text;
                wordInfos.RemoveAt(i + 1);
                i--;
            }
        }

        return wordInfos;
    }

    private List<WordInfo> CombineVerbDependant(List<WordInfo> wordInfos)
    {
        // Dependants
        for (int i = 1; i < wordInfos.Count; i++)
        {
            if (wordInfos[i].HasPartOfSpeechSection(PartOfSpeechSection.Dependant) &&
                wordInfos[i - 1].PartOfSpeech == PartOfSpeech.Verb)
            {
                wordInfos[i - 1].Text += wordInfos[i].Text;
                wordInfos.RemoveAt(i);
                i--;
            }
        }

        // Possible dependants, might need special rules?
        for (int i = 1; i < wordInfos.Count; i++)
        {
            if (wordInfos[i].HasPartOfSpeechSection(PartOfSpeechSection.PossibleDependant) &&
                wordInfos[i].DictionaryForm != "ござる" &&
                wordInfos[i - 1].PartOfSpeech == PartOfSpeech.Verb)
            {
                wordInfos[i - 1].Text += wordInfos[i].Text;
                wordInfos.RemoveAt(i);
                i--;
            }
        }

        // 注意してください
        for (int i = 0; i < wordInfos.Count - 1; i++)
        {
            if (wordInfos[i].HasPartOfSpeechSection(PartOfSpeechSection.PossibleSuru) &&
                wordInfos[i + 1].DictionaryForm == "する")
            {
                wordInfos[i].Text += wordInfos[i + 1].Text;
                wordInfos.RemoveAt(i + 1);
            }
        }

        return wordInfos;
    }
    
    private List<WordInfo> CombineAdverbialParticle(List<WordInfo> wordInfos)
    {
        // Dependants
        for (int i = 1; i < wordInfos.Count; i++)
        {
            // i.e　だり, たり
            if (wordInfos[i].HasPartOfSpeechSection(PartOfSpeechSection.AdverbialParticle) &&
                wordInfos[i - 1].PartOfSpeech == PartOfSpeech.Verb)
            {
                wordInfos[i - 1].Text += wordInfos[i].Text;
                wordInfos.RemoveAt(i);
                i--;
            }
        }

        return wordInfos;
    }

    private List<WordInfo> CombineConjunctiveParticle(List<WordInfo> wordInfos)
    {
        for (int i = 1; i < wordInfos.Count; i++)
        {
            if (wordInfos[i].HasPartOfSpeechSection(PartOfSpeechSection.ConjunctionParticle) &&
                wordInfos[i - 1].PartOfSpeech == PartOfSpeech.Verb)
            {
                wordInfos[i - 1].Text += wordInfos[i].Text;
                wordInfos.RemoveAt(i);
                i--;
            }
        }

        return wordInfos;
    }

    private List<WordInfo> CombineAuxiliary(List<WordInfo> wordInfos)
    {
        for (int i = 1; i < wordInfos.Count; i++)
        {
            if (wordInfos[i].PartOfSpeech == PartOfSpeech.Auxiliary && (wordInfos[i - 1].PartOfSpeech == PartOfSpeech.Verb ||
                                                                                 wordInfos[i - 1].PartOfSpeech == PartOfSpeech.IAdjective
                                                                               // || wordInfos[i-1].HasPartOfSpeechSection(PartOfSpeechSection.PossibleSuru)
                                                                                 ))
            {
                wordInfos[i - 1].Text += wordInfos[i].Text;
                wordInfos.RemoveAt(i);
                i--;
            }
        }

        return wordInfos;
    }

    private List<WordInfo> CombineAuxiliaryVerbStem(List<WordInfo> wordInfos)
    {
        for (int i = 1; i < wordInfos.Count; i++)
        {
            if (/*wordInfos[i].AnyPartOfSpeechSection(PartOfSpeechSection.Suffix) &&*/
                wordInfos[i].HasPartOfSpeechSection(PartOfSpeechSection.AuxiliaryVerbStem) &&
                (wordInfos[i - 1].PartOfSpeech == PartOfSpeech.Verb || wordInfos[i - 1].PartOfSpeech == PartOfSpeech.IAdjective))
            {
                wordInfos[i - 1].Text += wordInfos[i].Text;
                wordInfos.RemoveAt(i);
                i--;
            }
        }

        return wordInfos;
    }

    private List<WordInfo> CombineSuffix(List<WordInfo> wordInfos)
    {
        for (int i = 1; i < wordInfos.Count; i++)
        {
            if ((wordInfos[i].PartOfSpeech == PartOfSpeech.Suffix || wordInfos[i].HasPartOfSpeechSection(PartOfSpeechSection.Suffix)) && (wordInfos[i].DictionaryForm != "たち" || wordInfos[i-1].PartOfSpeech == PartOfSpeech.Pronoun))
            {
                wordInfos[i - 1].Text += wordInfos[i].Text;
                wordInfos.RemoveAt(i);
                i--;
            }
        }

        return wordInfos;
    }

    private List<WordInfo> CombineParticles(List<WordInfo> wordInfos)
    {
        for (int i = 0; i < wordInfos.Count - 1; i++)
        {
            
            // には
            if (wordInfos[i].Text == "に" && wordInfos[i + 1].Text == "は")
            {
                wordInfos[i].Text = "には";
                wordInfos.RemoveAt(i + 1);
            }

            // とは
            if (wordInfos[i].Text == "と" && wordInfos[i + 1].Text == "は")
            {
                wordInfos[i].Text = "とは";
                wordInfos.RemoveAt(i + 1);
            }
            
            // では
            if (wordInfos[i].Text == "で" && wordInfos[i + 1].Text == "は")
            {
                wordInfos[i].Text = "では";
                wordInfos.RemoveAt(i + 1);
            }
            
            // のに
            if (wordInfos[i].Text == "の" && wordInfos[i + 1].Text == "に")
            {
                wordInfos[i].Text = "のに";
                wordInfos.RemoveAt(i + 1);
            }
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
        for (int i = 1; i < wordInfos.Count; i++)
        {
            if (wordInfos[i].Text == "ば" &&
                wordInfos[i - 1].PartOfSpeech == PartOfSpeech.Verb)
            {
                wordInfos[i - 1].Text += wordInfos[i].Text;
                wordInfos.RemoveAt(i);
                i--;
            }
        }

        return wordInfos;
    }
}