﻿using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using WanaKanaShaapu;

namespace Jiten.Core.Data.JMDict;

public static class JmDictHelper
{
    private static readonly Dictionary<string, string> _entities = new Dictionary<string, string>();

    private static readonly Dictionary<string, string> _posDictionary = new()
                                                                        {
                                                                            { "bra", "Brazilian" }, { "hob", "Hokkaido-ben" },
                                                                            { "ksb", "Kansai-ben" }, { "ktb", "Kantou-ben" },
                                                                            { "kyb", "Kyoto-ben" }, { "kyu", "Kyuushuu-ben" },
                                                                            { "nab", "Nagano-ben" }, { "osb", "Osaka-ben" },
                                                                            { "rkb", "Ryuukyuu-ben" }, { "thb", "Touhoku-ben" },
                                                                            { "tsb", "Tosa-ben" }, { "tsug", "Tsugaru-ben" },
                                                                            { "agric", "agriculture" }, { "anat", "anatomy" },
                                                                            { "archeol", "archeology" }, { "archit", "architecture" },
                                                                            { "art", "art, aesthetics" }, { "astron", "astronomy" },
                                                                            { "audvid", "audiovisual" }, { "aviat", "aviation" },
                                                                            { "baseb", "baseball" }, { "biochem", "biochemistry" },
                                                                            { "biol", "biology" }, { "bot", "botany" },
                                                                            { "Buddh", "Buddhism" }, { "bus", "business" },
                                                                            { "cards", "card games" }, { "chem", "chemistry" },
                                                                            { "Christn", "Christianity" }, { "cloth", "clothing" },
                                                                            { "comp", "computing" }, { "cryst", "crystallography" },
                                                                            { "dent", "dentistry" }, { "ecol", "ecology" },
                                                                            { "econ", "economics" }, { "elec", "electricity, elec. eng." },
                                                                            { "electr", "electronics" }, { "embryo", "embryology" },
                                                                            { "engr", "engineering" }, { "ent", "entomology" },
                                                                            { "film", "film" }, { "finc", "finance" },
                                                                            { "fish", "fishing" }, { "food", "food, cooking" },
                                                                            { "gardn", "gardening, horticulture" }, { "genet", "genetics" },
                                                                            { "geogr", "geography" }, { "geol", "geology" },
                                                                            { "geom", "geometry" }, { "go", "go (game)" },
                                                                            { "golf", "golf" }, { "gramm", "grammar" },
                                                                            { "grmyth", "Greek mythology" }, { "hanaf", "hanafuda" },
                                                                            { "horse", "horse racing" }, { "kabuki", "kabuki" },
                                                                            { "law", "law" }, { "ling", "linguistics" },
                                                                            { "logic", "logic" }, { "MA", "martial arts" },
                                                                            { "mahj", "mahjong" }, { "manga", "manga" },
                                                                            { "math", "mathematics" }, { "mech", "mechanical engineering" },
                                                                            { "med", "medicine" }, { "met", "meteorology" },
                                                                            { "mil", "military" }, { "mining", "mining" },
                                                                            { "music", "music" }, { "noh", "noh" },
                                                                            { "ornith", "ornithology" }, { "paleo", "paleontology" },
                                                                            { "pathol", "pathology" }, { "pharm", "pharmacology" },
                                                                            { "phil", "philosophy" }, { "photo", "photography" },
                                                                            { "physics", "physics" }, { "physiol", "physiology" },
                                                                            { "politics", "politics" }, { "print", "printing" },
                                                                            { "psy", "psychiatry" }, { "psyanal", "psychoanalysis" },
                                                                            { "psych", "psychology" }, { "rail", "railway" },
                                                                            { "rommyth", "Roman mythology" }, { "Shinto", "Shinto" },
                                                                            { "shogi", "shogi" }, { "ski", "skiing" },
                                                                            { "sports", "sports" }, { "stat", "statistics" },
                                                                            { "stockm", "stock market" }, { "sumo", "sumo" },
                                                                            { "telec", "telecommunications" }, { "tradem", "trademark" },
                                                                            { "tv", "television" }, { "vidg", "video games" },
                                                                            { "zool", "zoology" }, { "abbr", "abbreviation" },
                                                                            { "arch", "archaic" }, { "char", "character" },
                                                                            { "chn", "children's language" }, { "col", "colloquial" },
                                                                            { "company", "company name" }, { "creat", "creature" },
                                                                            { "dated", "dated term" }, { "dei", "deity" },
                                                                            { "derog", "derogatory" }, { "doc", "document" },
                                                                            { "euph", "euphemistic" }, { "ev", "event" },
                                                                            { "fam", "familiar language" },
                                                                            { "fem", "female term or language" }, { "fict", "fiction" },
                                                                            { "form", "formal or literary term" },
                                                                            { "given", "given name or forename, gender not specified" },
                                                                            { "group", "group" }, { "hist", "historical term" },
                                                                            { "hon", "honorific or respectful (sonkeigo)" },
                                                                            { "hum", "humble (kenjougo)" },
                                                                            { "id", "idiomatic expression" },
                                                                            { "joc", "jocular, humorous term" }, { "leg", "legend" },
                                                                            { "m-sl", "manga slang" }, { "male", "male term or language" },
                                                                            { "myth", "mythology" }, { "net-sl", "Internet slang" },
                                                                            { "obj", "object" }, { "obs", "obsolete term" },
                                                                            { "on-mim", "onomatopoeic or mimetic" },
                                                                            { "organization", "organization name" }, { "oth", "other" },
                                                                            { "person", "full name of a particular person" },
                                                                            { "place", "place name" }, { "poet", "poetical term" },
                                                                            { "pol", "polite (teineigo)" }, { "product", "product name" },
                                                                            { "proverb", "proverb" }, { "quote", "quotation" },
                                                                            { "rare", "rare term" }, { "relig", "religion" },
                                                                            { "sens", "sensitive" }, { "serv", "service" },
                                                                            { "ship", "ship name" }, { "sl", "slang" },
                                                                            { "station", "railway station" },
                                                                            { "surname", "family or surname" },
                                                                            { "uk", "usually written using kana" },
                                                                            { "unclass", "unclassified name" }, { "vulg", "vulgar" },
                                                                            { "work", "work of art, literature, music, etc. name" },
                                                                            {
                                                                                "X",
                                                                                "rude or X-rated term (not displayed in educational software)"
                                                                            },
                                                                            { "yoji", "yojijukugo" },
                                                                            { "adj-f", "noun or verb acting prenominally" },
                                                                            { "adj-i", "adjective (keiyoushi)" },
                                                                            { "adj-ix", "adjective (keiyoushi) - yoi/ii class" },
                                                                            { "adj-kari", "'kari' adjective (archaic)" },
                                                                            { "adj-ku", "'ku' adjective (archaic)" },
                                                                            {
                                                                                "adj-na",
                                                                                "adjectival nouns or quasi-adjectives (keiyodoshi)"
                                                                            },
                                                                            { "adj-nari", "archaic/formal form of na-adjective" },
                                                                            {
                                                                                "adj-no",
                                                                                "nouns which may take the genitive case particle 'no'"
                                                                            },
                                                                            { "adj-pn", "pre-noun adjectival (rentaishi)" },
                                                                            { "adj-shiku", "'shiku' adjective (archaic)" },
                                                                            { "adj-t", "'taru' adjective" }, { "adv", "adverb (fukushi)" },
                                                                            { "adv-to", "adverb taking the 'to' particle" },
                                                                            { "aux", "auxiliary" }, { "aux-adj", "auxiliary adjective" },
                                                                            { "aux-v", "auxiliary verb" }, { "conj", "conjunction" },
                                                                            { "cop", "copula" }, { "ctr", "counter" },
                                                                            { "exp", "expressions (phrases, clauses, etc.)" },
                                                                            { "int", "interjection (kandoushi)" },
                                                                            { "n", "noun (common) (futsuumeishi)" },
                                                                            { "n-adv", "adverbial noun (fukushitekimeishi)" },
                                                                            { "n-pr", "proper noun" },
                                                                            { "n-pref", "noun, used as a prefix" },
                                                                            { "n-suf", "noun, used as a suffix" },
                                                                            { "n-t", "noun (temporal) (jisoumeishi)" },
                                                                            { "num", "numeric" }, { "pn", "pronoun" }, { "pref", "prefix" },
                                                                            { "prt", "particle" }, { "suf", "suffix" },
                                                                            { "unc", "unclassified" }, { "v-unspec", "verb unspecified" },
                                                                            { "v1", "Ichidan verb" },
                                                                            { "v1-s", "Ichidan verb - kureru special class" },
                                                                            { "v2a-s", "Nidan verb with 'u' ending (archaic)" },
                                                                            {
                                                                                "v2b-k",
                                                                                "Nidan verb (upper class) with 'bu' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2b-s",
                                                                                "Nidan verb (lower class) with 'bu' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2d-k",
                                                                                "Nidan verb (upper class) with 'dzu' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2d-s",
                                                                                "Nidan verb (lower class) with 'dzu' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2g-k",
                                                                                "Nidan verb (upper class) with 'gu' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2g-s",
                                                                                "Nidan verb (lower class) with 'gu' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2h-k",
                                                                                "Nidan verb (upper class) with 'hu/fu' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2h-s",
                                                                                "Nidan verb (lower class) with 'hu/fu' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2k-k",
                                                                                "Nidan verb (upper class) with 'ku' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2k-s",
                                                                                "Nidan verb (lower class) with 'ku' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2m-k",
                                                                                "Nidan verb (upper class) with 'mu' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2m-s",
                                                                                "Nidan verb (lower class) with 'mu' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2n-s",
                                                                                "Nidan verb (lower class) with 'nu' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2r-k",
                                                                                "Nidan verb (upper class) with 'ru' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2r-s",
                                                                                "Nidan verb (lower class) with 'ru' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2s-s",
                                                                                "Nidan verb (lower class) with 'su' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2t-k",
                                                                                "Nidan verb (upper class) with 'tsu' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2t-s",
                                                                                "Nidan verb (lower class) with 'tsu' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2w-s",
                                                                                "Nidan verb (lower class) with 'u' ending and 'we' conjugation (archaic)"
                                                                            },
                                                                            {
                                                                                "v2y-k",
                                                                                "Nidan verb (upper class) with 'yu' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2y-s",
                                                                                "Nidan verb (lower class) with 'yu' ending (archaic)"
                                                                            },
                                                                            {
                                                                                "v2z-s",
                                                                                "Nidan verb (lower class) with 'zu' ending (archaic)"
                                                                            },
                                                                            { "v4b", "Yodan verb with 'bu' ending (archaic)" },
                                                                            { "v4g", "Yodan verb with 'gu' ending (archaic)" },
                                                                            { "v4h", "Yodan verb with 'hu/fu' ending (archaic)" },
                                                                            { "v4k", "Yodan verb with 'ku' ending (archaic)" },
                                                                            { "v4m", "Yodan verb with 'mu' ending (archaic)" },
                                                                            { "v4n", "Yodan verb with 'nu' ending (archaic)" },
                                                                            { "v4r", "Yodan verb with 'ru' ending (archaic)" },
                                                                            { "v4s", "Yodan verb with 'su' ending (archaic)" },
                                                                            { "v4t", "Yodan verb with 'tsu' ending (archaic)" },
                                                                            { "v5aru", "Godan verb - -aru special class" },
                                                                            { "v5b", "Godan verb with 'bu' ending" },
                                                                            { "v5g", "Godan verb with 'gu' ending" },
                                                                            { "v5k", "Godan verb with 'ku' ending" },
                                                                            { "v5k-s", "Godan verb - Iku/Yuku special class" },
                                                                            { "v5m", "Godan verb with 'mu' ending" },
                                                                            { "v5n", "Godan verb with 'nu' ending" },
                                                                            { "v5r", "Godan verb with 'ru' ending" },
                                                                            { "v5r-i", "Godan verb with 'ru' ending (irregular verb)" },
                                                                            { "v5s", "Godan verb with 'su' ending" },
                                                                            { "v5t", "Godan verb with 'tsu' ending" },
                                                                            { "v5u", "Godan verb with 'u' ending" },
                                                                            { "v5u-s", "Godan verb with 'u' ending (special class)" },
                                                                            {
                                                                                "v5uru", "Godan verb - Uru old class verb (old form of Eru)"
                                                                            },
                                                                            { "vi", "intransitive verb" },
                                                                            { "vk", "Kuru verb - special class" },
                                                                            { "vn", "irregular nu verb" },
                                                                            { "vr", "irregular ru verb, plain form ends with -ri" },
                                                                            { "vs", "noun or participle which takes the aux. verb suru" },
                                                                            { "vs-c", "su verb - precursor to the modern suru" },
                                                                            { "vs-i", "suru verb - included" },
                                                                            { "vs-s", "suru verb - special class" },
                                                                            { "vt", "transitive verb" },
                                                                            {
                                                                                "vz",
                                                                                "Ichidan verb - zuru verb (alternative form of -jiru verbs)"
                                                                            },
                                                                            {
                                                                                "gikun",
                                                                                "gikun (meaning as reading) or jukujikun (special kanji reading)"
                                                                            },
                                                                            { "ik", "irregular kana usage" },
                                                                            { "ok", "out-dated or obsolete kana usage" },
                                                                            { "sk", "search-only kana form" },
                                                                        };

    public static async Task<List<JmDictWord>> LoadAllWords(JitenDbContext context)
    {
        context.ChangeTracker.AutoDetectChangesEnabled = false;
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        return await context.JMDictWords
                            .AsNoTracking()
                            .ToListAsync();
    }


    public static async Task<Dictionary<string, List<int>>> LoadLookupTable(JitenDbContext context)
    {
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

    public static List<string> ToHumanReadablePartsOfSpeech(this List<string> pos)
    {
        List<string> humanReadablePos = new();
        foreach (var p in pos)
        {
            humanReadablePos.Add(_posDictionary.GetValueOrDefault(p, p));
        }

        return humanReadablePos;
    }


    public static async Task<bool> Import(DbContextOptions<JitenDbContext> options, string dtdPath, string dictionaryPath,
                                          string furiganaPath)
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

        var readerSettings = new XmlReaderSettings() { Async = true, DtdProcessing = DtdProcessing.Parse, MaxCharactersFromEntities = 0 };
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

                wordInfo.Readings = wordInfo.Readings.Select(r => r.Replace("ゎ", "わ").Replace("ヮ", "わ")).ToList();

                wordInfos.Add(wordInfo);

                break;
            }
        }

        reader.Close();

        wordInfos.AddRange(GetCustomWords());

        var furiganas = await JsonSerializer.DeserializeAsync<List<JMDictFurigana>>(File.OpenRead(furiganaPath));
        Dictionary<string, List<JMDictFurigana>> furiganaDict = new();
        foreach (var f in furiganas!)
        {
            // Store all furiganas with the same key
            if (!furiganaDict.TryGetValue(f.Text, out var list))
            {
                list = new List<JMDictFurigana>();
                furiganaDict.Add(f.Text, list);
            }

            list.Add(f);
        }

        await using var context = new JitenDbContext(options);
        foreach (var reading in wordInfos)
        {
            List<JmDictLookup> lookups = new();
            reading.ReadingsFurigana = new List<string>();

            for (var i = 0; i < reading.Readings.Count; i++)
            {
                string? r = reading.Readings[i];
                var lookupKey = WanaKana.ToHiragana(r.Replace("ゎ", "わ").Replace("ヮ", "わ"),
                                                    new DefaultOptions() { ConvertLongVowelMark = false });
                var lookupKeyWithoutLongVowelMark = WanaKana.ToHiragana(r.Replace("ゎ", "わ").Replace("ヮ", "わ"));

                if (!lookups.Any(l => l.WordId == reading.WordId && l.LookupKey == lookupKey))
                {
                    lookups.Add(new JmDictLookup { WordId = reading.WordId, LookupKey = lookupKey });
                }

                if (lookupKeyWithoutLongVowelMark != lookupKey &&
                    !lookups.Any(l => l.WordId == reading.WordId && l.LookupKey == lookupKeyWithoutLongVowelMark))
                {
                    lookups.Add(new JmDictLookup { WordId = reading.WordId, LookupKey = lookupKeyWithoutLongVowelMark });
                }

                // For single kanjis only words, the furigana deck will probably be wrong, so we need an alternative
                if (r.Length == 1 && WanaKana.IsKanji(r))
                {
                    reading.ReadingsFurigana.Add($"{r}[{reading.Readings.First(WanaKana.IsKana)}]");
                }
                else
                {
                    string? furiReading = null;

                    // Try to find a matching furigana
                    if (furiganaDict.TryGetValue(r, out var furiList) && furiList.Count > 0)
                    {
                        // Try to match one of the furiganas with the readings
                        foreach (var furi in furiList)
                        {
                            // Check if the reading matches with any of the readings in the word
                            if (reading.Readings.Contains(furi.Reading))
                            {
                                furiReading = furi.Parse();
                                reading.ReadingsFurigana.Add(furiReading ?? reading.Readings[i]);
                                break;
                            }
                        }

                        // If no match found, error instead
                        if (furiReading == null)
                        {
                            Console.WriteLine($"No furigana found for reading {r}");
                        }
                    }
                }
            }

            reading.PartsOfSpeech = reading.Definitions.SelectMany(d => d.PartsOfSpeech).Distinct().ToList();
            reading.Lookups = lookups;
        }

        // custom priorities
        wordInfos.First(w => w.WordId == 1332650).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 2848543).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1160790).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1203260).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1397260).Priorities?.Add("jiten");
        
        context.JMDictWords.AddRange(wordInfos);
        
        await context.SaveChangesAsync();

        return true;
    }

    private static async Task<JmDictWord> ParseKEle(XmlReader reader, JmDictWord wordInfo)
    {
        if (reader.Name != "k_ele") return wordInfo;

        while (await reader.ReadAsync())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                if (reader.Name == "keb")
                {
                    var keb = await reader.ReadElementContentAsStringAsync();
                    wordInfo.Readings.Add(keb);
                    wordInfo.ReadingTypes.Add(JmDictReadingType.Reading);
                }

                if (reader.Name == "ke_pri")
                {
                    var pri = await reader.ReadElementContentAsStringAsync();
                    if (!wordInfo.Priorities.Contains(pri))
                        wordInfo.Priorities.Add(pri);
                }
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
        bool isObsolete = false;
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

                if (reader.Name == "re_inf")
                {
                    var inf = await reader.ReadElementContentAsStringAsync();
                    if (inf.ToLower() == "&ok")
                        isObsolete = true;
                }

                if (reader.Name == "re_pri")
                {
                    var pri = await reader.ReadElementContentAsStringAsync();
                    if (!wordInfo.Priorities.Contains(pri))
                        wordInfo.Priorities.Add(pri);
                }
            }

            if (reader.NodeType != XmlNodeType.EndElement) continue;
            if (reader.Name != "r_ele") continue;

            if (restrictions.Count == 0 || wordInfo.Readings.Any(reading => restrictions.Contains(reading)))
            {
                if (isObsolete)
                {
                    wordInfo.ObsoleteReadings?.Add(reb);
                }
                else
                {
                    wordInfo.Readings.Add(reb);
                    wordInfo.ReadingTypes.Add(JmDictReadingType.KanaReading);
                }
            }

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
                            //sense.EnglishMeanings.Add(await reader.ReadElementContentAsStringAsync());
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

    private static List<JmDictWord> GetCustomWords()
    {
        var customWordInfos = new List<JmDictWord>();

        customWordInfos.Add(new JmDictWord
                            {
                                WordId = 8000000, Readings = new List<string> { "でした" }, ReadingTypes = [JmDictReadingType.KanaReading],
                                Definitions =
                                [
                                    new JmDictDefinition { EnglishMeanings = ["was, were"], PartsOfSpeech = ["exp"] }
                                ]
                            });

        return customWordInfos;
    }

    public static async Task<bool> ImportPitchAccents(bool verbose, DbContextOptions<JitenDbContext> options,
                                                      string pitchAcentsDirectoryPath)
    {
        if (!Directory.Exists(pitchAcentsDirectoryPath))
        {
            Console.WriteLine($"Directory {pitchAcentsDirectoryPath} does not exist.");
            return false;
        }

        var pitchAccentFiles = Directory.GetFiles(pitchAcentsDirectoryPath, "term_meta_bank_*.json");

        if (pitchAccentFiles.Length == 0)
        {
            Console.WriteLine($"No pitch accent files found in {pitchAcentsDirectoryPath}. The files should be named term_meta_bank_*.json");
            return false;
        }

        var pitchAccentDict = new Dictionary<string, List<int>>();

        foreach (var file in pitchAccentFiles)
        {
            string jsonContent = await File.ReadAllTextAsync(file);
            using JsonDocument doc = JsonDocument.Parse(jsonContent);

            foreach (JsonElement item in doc.RootElement.EnumerateArray())
            {
                string? word = item[0].GetString();

                if (word == null)
                    continue;

                string? type = item[1].GetString();

                JsonElement pitchInfo = item[2];
                string? reading = pitchInfo.GetProperty("reading").GetString();

                List<int> positions = new();
                foreach (JsonElement pitch in pitchInfo.GetProperty("pitches").EnumerateArray())
                {
                    positions.Add(pitch.GetProperty("position").GetInt32());
                }

                pitchAccentDict.TryAdd(word, positions);
            }
        }

        if (verbose)
            Console.WriteLine($"Found {pitchAccentDict.Count()} pitch accent records.");

        var context = new JitenDbContext(options);
        var allWords = await context.JMDictWords.ToListAsync();
        int wordsUpdated = 0;

        for (var i = 0; i < allWords.Count; i++)
        {
            if (verbose && i % 10000 == 0)
                Console.WriteLine($"Processing word {i + 1}/{allWords.Count} ({(i + 1) * 100 / allWords.Count}%)");

            var word = allWords[i];

            foreach (var reading in word.Readings)
            {
                if (pitchAccentDict.TryGetValue(reading, out var pitchAccents))
                {
                    word.PitchAccents = pitchAccents;

                    wordsUpdated++;
                    break; // Stop at the first match
                }
            }
        }

        if (verbose)
            Console.WriteLine($"Updated pitch accents for {wordsUpdated} words. Saving to database...");

        await context.SaveChangesAsync();
        return true;
    }
}