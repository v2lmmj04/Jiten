using System.Text.RegularExpressions;
using System.Xml;
using JapaneseParser.DictionaryTools;
using Microsoft.EntityFrameworkCore;
using WanaKanaShaapu;

public class JMDictHelper
{
    private static Dictionary<string, string> _entities = new Dictionary<string, string>();
    
    public static async Task<List<DbJMDictWordInfo>> LoadAllWords()
    {
        using var context = new JMDictDbContext();

        return await context.JMDictWords
                            .ToListAsync();
    }


    public static async Task<Dictionary<string, List<int>>> LoadLookupTable()
    {
        using var context = new JMDictDbContext();

        return await context.Lookups.GroupBy(l => l.LookupKey)
                            .ToDictionaryAsync(g => g.Key, g => g.Select(l => l.EntrySequenceId).ToList());
    }


    public static async Task<bool> Import(string dtd_path, string dictionaryPath)
    {
        Regex reg = new Regex(@"<!ENTITY (.*) ""(.*)"">");

        foreach (var line in await File.ReadAllLinesAsync(dtd_path))
        {
            var matches = reg.Match(line);
            if (matches.Length > 0)
            {
                _entities.Add(matches.Groups[1].Value, matches.Groups[2].Value);
            }
        }

        var readerSettings = new XmlReaderSettings() { Async = true, DtdProcessing = DtdProcessing.Parse };
        XmlReader reader = XmlReader.Create(dictionaryPath, readerSettings);

        await reader.MoveToContentAsync();

        List<JMDictWordInfo> wordInfos = new();

        while (await reader.ReadAsync())
        {
            if (reader.NodeType != XmlNodeType.Element) continue;

            if (reader.Name != "entry") continue;

            int entrySequence = 0;

            var wordInfo = new JMDictWordInfo();

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "ent_seq")
                    {
                        entrySequence = reader.ReadElementContentAsInt();
                        wordInfo.EntrySequenceId = entrySequence;
                    }

                    wordInfo = await ParseKEle(reader, wordInfo);
                    wordInfo = await ParseREle(reader, wordInfo);
                    wordInfo = await ParseSense(reader, wordInfo);
                }

                if (reader.NodeType != XmlNodeType.EndElement) continue;
                if (reader.Name != "entry") continue;

                wordInfos.Add(wordInfo);

                break;
            }
        }

        reader.Close();


        using var context = new JMDictDbContext();
        foreach (var reading in wordInfos)
        {
            List<DbLookup> lookups = new();


            foreach (var r in reading.Readings)
            {
                var lookupKey = WanaKana.ToHiragana(r.Replace("ゎ", "わ").Replace("ヮ", "わ"));
                if (!lookups.Any(l => l.EntrySequenceId == reading.EntrySequenceId && l.LookupKey == lookupKey))
                {
                    lookups.Add(new DbLookup { EntrySequenceId = reading.EntrySequenceId, LookupKey = lookupKey });
                }
            }

            foreach (var k in reading.KanaReading)
            {
                var lookupKey = WanaKana.ToHiragana(k.Replace("ゎ", "わ").Replace("ヮ", "わ"));
                if (!lookups.Any(l => l.EntrySequenceId == reading.EntrySequenceId && l.LookupKey == lookupKey))
                {
                    lookups.Add(new DbLookup { EntrySequenceId = reading.EntrySequenceId, LookupKey = lookupKey });
                }
            }

            var dbWordInfo = new DbJMDictWordInfo
                             {
                                 EntrySequenceId = reading.EntrySequenceId,
                                 Readings = reading.Readings,
                                 KanaReadings = reading.KanaReading,
                                 PartsOfSpeech = reading.Definitions.SelectMany(d => d.PartsOfSpeech).Distinct().ToList(),
                                 Definitions = reading.Definitions.Select(d => new DbDefinition
                                                                               {
                                                                                   PartsOfSpeech = d.PartsOfSpeech,
                                                                                   EnglishMeanings = d.EnglishMeanings,
                                                                                   DutchMeanings = d.DutchMeanings,
                                                                                   FrenchMeanings = d.FrenchMeanings,
                                                                                   GermanMeanings = d.GermanMeanings,
                                                                                   SpanishMeanings = d.SpanishMeanings,
                                                                                   HungarianMeanings = d.HungarianMeanings,
                                                                                   RussianMeanings = d.RussianMeanings,
                                                                                   SlovenianMeanings = d.SlovenianMeanings
                                                                               }).ToList(),
                                 Lookups = lookups
                             };

            context.JMDictWords.Add(dbWordInfo);
        }

        context.ChangeTracker.AutoDetectChangesEnabled = false;
        await context.SaveChangesAsync();

        return true;
    }

    private static async Task<JMDictWordInfo> ParseKEle(XmlReader reader, JMDictWordInfo wordInfo)
    {
        if (reader.Name == "k_ele")
        {
            while (await reader.ReadAsync())
            {
                if (reader is { NodeType: XmlNodeType.Element, Name: "keb" })
                {
                    var keb = await reader.ReadElementContentAsStringAsync();
                    wordInfo.Readings.Add(keb);
                }

                if (reader.NodeType != XmlNodeType.EndElement) continue;
                if (reader.Name != "k_ele") continue;

                break;
            }
        }

        return wordInfo;
    }

    private static async Task<JMDictWordInfo> ParseREle(XmlReader reader, JMDictWordInfo wordInfo)
    {
        if (reader.Name == "r_ele")
        {
            string reb = "";
            List<string> restrictions = new List<string>();
            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "reb")
                    {
                        reb = await reader.ReadElementContentAsStringAsync();
                    }

                    if (reader.Name == "re_restr")
                    {
                        restrictions.Add(await reader.ReadElementContentAsStringAsync());
                    }
                }

                if (reader.NodeType != XmlNodeType.EndElement) continue;
                if (reader.Name != "r_ele") continue;

                if (restrictions.Count == 0 || wordInfo.Readings.Any(reading => restrictions.Contains(reading)))
                    wordInfo.KanaReading.Add(reb);

                break;
            }
        }

        return wordInfo;
    }

    private static async Task<JMDictWordInfo> ParseSense(XmlReader reader, JMDictWordInfo wordInfo)
    {
        if (reader.Name == "sense")
        {
            var sense = new Definition();
            List<string> restrictions = new List<string>();

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "stagr")
                    {
                        restrictions.Add(await reader.ReadElementContentAsStringAsync());
                    }

                    if (reader is { Name: "gloss", HasAttributes: true })
                        // check the language attribute
                    {
                        var attribute = reader.GetAttribute("xml:lang");
                        switch (attribute)
                        {
                            case "eng":
                                sense.EnglishMeanings.Add(await reader.ReadElementContentAsStringAsync());
                                break;
                            case "dut":
                                sense.DutchMeanings.Add(await reader.ReadElementContentAsStringAsync());
                                break;
                            case "fre":
                                sense.FrenchMeanings.Add(await reader.ReadElementContentAsStringAsync());
                                break;
                            case "ger":
                                sense.GermanMeanings.Add(await reader.ReadElementContentAsStringAsync());
                                break;
                            case "spa":
                                sense.SpanishMeanings.Add(await reader.ReadElementContentAsStringAsync());
                                break;
                            case "hun":
                                sense.HungarianMeanings.Add(await reader.ReadElementContentAsStringAsync());
                                break;
                            case "rus":
                                sense.RussianMeanings.Add(await reader.ReadElementContentAsStringAsync());
                                break;
                            case "slv":
                                sense.SlovenianMeanings.Add(await reader.ReadElementContentAsStringAsync());
                                break;
                            default:
                                sense.EnglishMeanings.Add(await reader.ReadElementContentAsStringAsync());
                                break;
                        }
                    }

                    if (reader.Name == "pos")
                    {
                        var el = reader.ReadElementString();

                        sense.PartsOfSpeech.Add(ElToPos(el));
                    }

                    if (reader.Name == "misc")
                    {
                        var el = reader.ReadElementString();

                        sense.PartsOfSpeech.Add(ElToPos(el));
                    }
                }

                if (reader.NodeType != XmlNodeType.EndElement) continue;
                if (reader.Name != "sense") continue;

                if (restrictions.Count == 0 || wordInfo.Readings.Any(reading => restrictions.Contains(reading)))
                    wordInfo.Definitions.Add(sense);

                break;
            }
        }

        return wordInfo;
    }

    private static string ElToPos(string el)
    {
        return _entities.First(e => e.Value == el).Key;
    }
}