using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using WanaKanaShaapu;
using Z.EntityFramework.Plus;

namespace Jiten.Core.Data.JMDict;

public static class JmDictHelper
{
    private static readonly Dictionary<string, string> _entities = new Dictionary<string, string>();

    public static async Task InsertDeck(Deck deck)
    {
        await using var context = new JitenDbContext();

        context.Decks.Add(deck);
        
        await context.SaveChangesAsync();
    }
    
    public static async Task<List<JmDictWord>> LoadAllWords()
    {
        await using var context = new JitenDbContext();
        context.ChangeTracker.AutoDetectChangesEnabled = false;
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        
        return await context.JMDictWords
                            .AsNoTracking()
                            .ToListAsync();
    }


    public static async Task<Dictionary<string, List<int>>> LoadLookupTable()
    {
        await using var context = new JitenDbContext();
        var lookupTable = new Dictionary<string, List<int>>();

        await foreach (var lookup in context.Lookups.AsNoTracking().AsAsyncEnumerable())
        {
            if (!lookupTable.TryGetValue(lookup.LookupKey, out var wordIds))
            {
                wordIds = new List<int>();
                lookupTable[lookup.LookupKey] = wordIds;
            }
            wordIds.Add(lookup.WordId);
        }
        
        return lookupTable;
    }


    public static async Task<bool> Import(string dtdPath, string dictionaryPath)
    {
        Regex reg = new Regex(@"<!ENTITY (.*) ""(.*)"">");

        foreach (var line in await File.ReadAllLinesAsync(dtdPath))
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

        List<JmDictWord> wordInfos = new();

        while (await reader.ReadAsync())
        {
            if (reader.NodeType != XmlNodeType.Element) continue;

            if (reader.Name != "entry") continue;

            var wordInfo = new JmDictWord();

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "ent_seq")
                        wordInfo.WordId = reader.ReadElementContentAsInt();

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
        
        await using var context = new JitenDbContext();
        foreach (var reading in wordInfos)
        {
            List<JmDictLookup> lookups = new();

            foreach (var r in reading.Readings)
            {
                var lookupKey = WanaKana.ToHiragana(r.Replace("ゎ", "わ").Replace("ヮ", "わ"));
                if (!lookups.Any(l => l.WordId == reading.WordId && l.LookupKey == lookupKey))
                {
                    lookups.Add(new JmDictLookup { WordId = reading.WordId, LookupKey = lookupKey });
                }
            }

            foreach (var k in reading.KanaReadings)
            {
                var lookupKey = WanaKana.ToHiragana(k.Replace("ゎ", "わ").Replace("ヮ", "わ"));
                if (!lookups.Any(l => l.WordId == reading.WordId && l.LookupKey == lookupKey))
                {
                    lookups.Add(new JmDictLookup { WordId = reading.WordId, LookupKey = lookupKey });
                }
            }

            reading.PartsOfSpeech = reading.Definitions.SelectMany(d => d.PartsOfSpeech).Distinct().ToList();
            reading.Lookups = lookups;
        }

        context.ChangeTracker.AutoDetectChangesEnabled = false;
        await context.BulkInsertOptimizedAsync(wordInfos, options => options.IncludeGraph = true);

        return true;
    }

    private static async Task<JmDictWord> ParseKEle(XmlReader reader, JmDictWord wordInfo)
    {
        if (reader.Name != "k_ele") return wordInfo;
        
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

        return wordInfo;
    }

    private static async Task<JmDictWord> ParseREle(XmlReader reader, JmDictWord wordInfo)
    {
        if (reader.Name != "r_ele") return wordInfo;
        
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
                wordInfo.KanaReadings.Add(reb);

            break;
        }

        return wordInfo;
    }

    private static async Task<JmDictWord> ParseSense(XmlReader reader, JmDictWord wordInfo)
    {
        if (reader.Name != "sense") return wordInfo;

        var sense = new JmDictDefinition();
        List<string> restrictions = new List<string>();

        while (await reader.ReadAsync())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                if (reader.Name == "stagr")
                {
                    restrictions.Add(await reader.ReadElementContentAsStringAsync());
                }

                // check the language attribute
                if (reader is { Name: "gloss", HasAttributes: true })
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

        return wordInfo;
    }

    private static string ElToPos(string el)
    {
        return _entities.First(e => e.Value == el).Key;
    }
}