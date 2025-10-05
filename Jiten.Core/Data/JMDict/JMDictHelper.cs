﻿using System.Globalization;
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
                                                                            // Name types from JMNedict
                                                                            { "name", "name" }, { "name-fem", "female name" },
                                                                            { "name-male", "male name" }, { "name-given", "given name" },
                                                                            { "name-surname", "surname" }, { "name-place", "place name" },
                                                                            { "name-person", "person name" },
                                                                            { "name-unclass", "unclassified name" },
                                                                            { "name-station", "station name" },
                                                                            { "name-organization", "organization name" },
                                                                            { "name-company", "company name" },
                                                                            { "name-product", "product name" },
                                                                            { "name-work", "work name" }, { "dent", "dentistry" },
                                                                            { "ecol", "ecology" }, { "econ", "economics" },
                                                                            { "elec", "electricity, elec. eng." },
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
                                                                            { "sk", "search-only kana form" }, { "boxing", "boxing" },
                                                                            { "chmyth", "Chinese mythology" },
                                                                            { "civeng", "civil engineering" },
                                                                            { "figskt", "figure skating" }, { "internet", "Internet" },
                                                                            { "jpmyth", "Japanese mythology" }, { "min", "mineralogy" },
                                                                            { "motor", "motorsport" },
                                                                            { "prowres", "professional wrestling" }, { "surg", "surgery" },
                                                                            { "vet", "veterinary terms" },
                                                                            { "ateji", "ateji (phonetic) reading" },
                                                                            // { "ik", "word containing irregular kana usage" },
                                                                            { "iK", "word containing irregular kanji usage" },
                                                                            { "io", "irregular okurigana usage" },
                                                                            { "oK", "word containing out-dated kanji or kanji usage" },
                                                                            { "rK", "rarely used kanji form" },
                                                                            { "sK", "search-only kanji form" },
                                                                            { "rk", "rarely used kana form" },
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
        var wordInfos = await GetWordInfos(dtdPath, dictionaryPath);

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

                if (WanaKana.IsKatakana(r) && !lookups.Any(l => l.WordId == reading.WordId && l.LookupKey == r))
                    lookups.Add(new JmDictLookup { WordId = reading.WordId, LookupKey = r });

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

                        // If no match found, show error and add the current reading instead
                        if (furiReading == null)
                        {
                            Console.WriteLine($"No furigana found for reading {r}");
                            reading.ReadingsFurigana.Add(reading.Readings[i]);
                        }
                    }
                    // Probably kana reading
                    else
                    {
                        reading.ReadingsFurigana.Add(reading.Readings[i]);
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
        wordInfos.First(w => w.WordId == 1499720).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1315130).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1315130).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1191730).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 2844190).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 2207630).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1442490).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1423310).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1502390).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1343100).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1610040).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 2059630).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1495580).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1288850).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1511350).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1648450).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1534790).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 2105530).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1223615).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1421850).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1020650).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1310640).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1495770).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1375610).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 1605840).Priorities?.Add("jiten");
        wordInfos.First(w => w.WordId == 2029110).Definitions.Add(new JmDictDefinition()
                                                                  {
                                                                      PartsOfSpeech = ["prt"], EnglishMeanings = ["indicates na-adjective"]
                                                                  });

        context.JMDictWords.AddRange(wordInfos);

        await context.SaveChangesAsync();

        return true;
    }

    public static async Task<bool> ImportJMNedict(DbContextOptions<JitenDbContext> options, string jmneDictPath)
    {
        Console.WriteLine("Starting JMNedict import...");

        var readerSettings = new XmlReaderSettings() { Async = true, DtdProcessing = DtdProcessing.Parse, MaxCharactersFromEntities = 0 };
        XmlReader reader = XmlReader.Create(jmneDictPath, readerSettings);

        await reader.MoveToContentAsync();

        // Dictionary to store entries by kanji element (keb) to combine entries with the same kanji
        Dictionary<string, JmDictWord> namesByKeb = new();

        await using var context = new JitenDbContext(options);

        // Load existing entries from JMDict to check for duplicates
        Console.WriteLine("Loading existing JMDict entries to check for duplicates...");
        var existingEntries = await LoadAllWords(context);
        var existingReadings = new HashSet<string>(existingEntries.SelectMany(e => e.Readings));
        Console.WriteLine($"Loaded {existingEntries.Count} existing entries with {existingReadings.Count} unique readings");

        while (await reader.ReadAsync())
        {
            if (reader.NodeType != XmlNodeType.Element) continue;
            if (reader.Name != "entry") continue;

            var nameEntry = new JmDictWord();
            string? primaryKeb = null;

            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "ent_seq")
                        nameEntry.WordId = reader.ReadElementContentAsInt();

                    // Parse kanji elements (k_ele)
                    if (reader.Name == "k_ele")
                    {
                        await ParseNameKEle(reader, nameEntry);
                        // Save the first kanji element as the primary key for grouping
                        if (primaryKeb == null && nameEntry.Readings.Count > 0)
                        {
                            primaryKeb = nameEntry.Readings[0];
                        }
                    }

                    // Parse reading elements (r_ele)
                    if (reader.Name == "r_ele")
                    {
                        await ParseNameREle(reader, nameEntry);
                    }

                    // Parse translation elements (trans)
                    if (reader.Name == "trans")
                    {
                        await ParseNameTrans(reader, nameEntry);
                    }
                }

                if (reader.NodeType != XmlNodeType.EndElement) continue;
                if (reader.Name != "entry") continue;

                nameEntry.Readings = nameEntry.Readings.Select(r => r.Replace("ゎ", "わ").Replace("ヮ", "わ")).ToList();

                // Check if this entry already exists in JMDict
                bool alreadyExists = nameEntry.Readings.Any(r => existingReadings.Contains(r));
                if (alreadyExists)
                {
                    // Skip this entry as it already exists in JMDict
                    break;
                }

                // If we have a primary kanji, check if we need to merge with an existing entry
                if (primaryKeb != null && nameEntry.Readings.Count > 0)
                {
                    if (namesByKeb.TryGetValue(primaryKeb, out var existingEntry))
                    {
                        // Merge this entry with the existing one
                        MergeNameEntries(existingEntry, nameEntry);
                    }
                    else
                    {
                        // Add as a new entry
                        namesByKeb[primaryKeb] = nameEntry;
                    }
                }
                else if (nameEntry.Readings.Count > 0)
                {
                    // If no kanji but has readings, use the first reading as key
                    string readingKey = nameEntry.Readings[0];
                    if (namesByKeb.TryGetValue(readingKey, out var existingEntry))
                    {
                        MergeNameEntries(existingEntry, nameEntry);
                    }
                    else
                    {
                        namesByKeb[readingKey] = nameEntry;
                    }
                }

                break;
            }
        }

        reader.Close();

        Console.WriteLine($"Processed {namesByKeb.Count} unique name entries after filtering out duplicates");

        // Process the merged name entries
        List<JmDictWord> nameWords = namesByKeb.Values.ToList();
        foreach (var nameWord in nameWords)
        {
            // Create lookups for searching
            List<JmDictLookup> lookups = new();
            nameWord.ReadingsFurigana = new List<string>();

            for (var i = 0; i < nameWord.Readings.Count; i++)
            {
                string? r = nameWord.Readings[i];
                var lookupKey = WanaKana.ToHiragana(r.Replace("ゎ", "わ").Replace("ヮ", "わ"),
                                                    new DefaultOptions() { ConvertLongVowelMark = false });
                var lookupKeyWithoutLongVowelMark = WanaKana.ToHiragana(r.Replace("ゎ", "わ").Replace("ヮ", "わ"));

                if (!lookups.Any(l => l.WordId == nameWord.WordId && l.LookupKey == lookupKey))
                {
                    lookups.Add(new JmDictLookup { WordId = nameWord.WordId, LookupKey = lookupKey });
                }

                if (lookupKeyWithoutLongVowelMark != lookupKey &&
                    !lookups.Any(l => l.WordId == nameWord.WordId && l.LookupKey == lookupKeyWithoutLongVowelMark))
                {
                    lookups.Add(new JmDictLookup { WordId = nameWord.WordId, LookupKey = lookupKeyWithoutLongVowelMark });
                }

                if (WanaKana.IsKatakana(r) && !lookups.Any(l => l.WordId == nameWord.WordId && l.LookupKey == r))
                    lookups.Add(new JmDictLookup { WordId = nameWord.WordId, LookupKey = r });

                // Populate furigana readings
                if (r.Length == 1 && WanaKana.IsKanji(r))
                {
                    // For single kanji, use kana reading as furigana
                    var kanaReading = nameWord.Readings.FirstOrDefault(WanaKana.IsKana);
                    nameWord.ReadingsFurigana.Add(kanaReading != null ? $"{r}[{kanaReading}]" : r);
                }
                else
                {
                    // For regular entries, just use the reading as is (no furigana data available)
                    nameWord.ReadingsFurigana.Add(nameWord.Readings[i]);
                }
            }

            // Set parts of speech from definitions (name types)
            nameWord.PartsOfSpeech = nameWord.Definitions.SelectMany(d => d.PartsOfSpeech).Distinct().ToList();
            nameWord.Lookups = lookups;

            // Add "name" priority to indicate it's from JMNedict
            if (nameWord.Priorities == null)
                nameWord.Priorities = new List<string>();
            nameWord.Priorities.Add("name");
        }

        if (nameWords.Count > 0)
        {
            // Add the processed name entries to the database
            context.JMDictWords.AddRange(nameWords);
            await context.SaveChangesAsync();

            Console.WriteLine($"Added {nameWords.Count} name entries to the database");
        }
        else
        {
            Console.WriteLine("No new name entries to add to the database");
        }

        return true;
    }

    public static async Task CompareJMDicts(string dtdPath, string dictionaryPathOld, string dictionaryPathNew)
    {
        var oldWordInfos = await GetWordInfos(dtdPath, dictionaryPathOld);
        var newWordInfos = await GetWordInfos(dtdPath, dictionaryPathNew);

        Console.WriteLine($"Words - Old dictionary: {oldWordInfos.Count}, New dictionary: {newWordInfos.Count}, difference (new - old): {newWordInfos.Count - oldWordInfos.Count}");

        // Check for duplicate WordIds in new dictionary and log them
        var duplicateWordIds = newWordInfos.GroupBy(w => w.WordId)
                                           .Where(g => g.Count() > 1)
                                           .Select(g => g.Key)
                                           .ToList();

        if (duplicateWordIds.Any())
        {
            Console.WriteLine($"Warning: Found {duplicateWordIds.Count} duplicate WordIds in the new dictionary.");
            foreach (var dupId in duplicateWordIds.Take(5))
            {
                var entries = newWordInfos.Where(w => w.WordId == dupId).ToList();
                Console.WriteLine($"  Duplicate ID: {dupId}, Readings: {string.Join(", ", entries.SelectMany(e => e.Readings))}");
            }

            if (duplicateWordIds.Count > 5)
                Console.WriteLine($"  ... and {duplicateWordIds.Count - 5} more");
        }

        // Create dictionaries with WordId as key for easier lookup, handling duplicates
        var oldWordDict = oldWordInfos.GroupBy(w => w.WordId)
                                      .ToDictionary(g => g.Key, g => g.First());

        var newWordDict = newWordInfos.GroupBy(w => w.WordId)
                                      .ToDictionary(g => g.Key, g => g.First());

        // Find added, removed, and changed words
        var addedWordIds = newWordDict.Keys.Except(oldWordDict.Keys).ToList();
        var removedWordIds = oldWordDict.Keys.Except(newWordDict.Keys).ToList();
        var commonWordIds = oldWordDict.Keys.Intersect(newWordDict.Keys).ToList();

        // Words with changes
        var changedWordIds = new List<int>();
        var readingChanges = new List<(int WordId, List<string> Added, List<string> Removed)>();
        var posChanges = new List<(int WordId, List<string> Added, List<string> Removed)>();
        var priorityChanges = new List<(int WordId, List<string> Added, List<string> Removed)>();

        // Check for changes in common words
        foreach (var wordId in commonWordIds)
        {
            var oldWord = oldWordDict[wordId];
            var newWord = newWordDict[wordId];
            bool isChanged = false;

            // Check for reading changes
            var oldReadings = oldWord.Readings;
            var newReadings = newWord.Readings;
            var addedReadings = newReadings.Except(oldReadings).ToList();
            var removedReadings = oldReadings.Except(newReadings).ToList();

            if (addedReadings.Any() || removedReadings.Any())
            {
                isChanged = true;
                readingChanges.Add((wordId, addedReadings, removedReadings));
            }


            // Check for parts of speech changes
            var oldPos = oldWord.Definitions.SelectMany(d => d.PartsOfSpeech).Distinct().ToList();
            ;
            var newPos = newWord.Definitions.SelectMany(d => d.PartsOfSpeech).Distinct().ToList();
            var addedPos = newPos.Except(oldPos).ToList();
            var removedPos = oldPos.Except(newPos).ToList();

            if (addedPos.Any() || removedPos.Any())
            {
                isChanged = true;
                posChanges.Add((wordId, addedPos, removedPos));
            }


            // Check for priority changes
            var oldPriorities = oldWord.Priorities ?? new List<string>();
            var newPriorities = newWord.Priorities ?? new List<string>();
            var addedPriorities = newPriorities.Except(oldPriorities).ToList();
            var removedPriorities = oldPriorities.Except(newPriorities).ToList();

            if (addedPriorities.Any() || removedPriorities.Any())
            {
                isChanged = true;
                priorityChanges.Add((wordId, addedPriorities, removedPriorities));
            }

            if (isChanged)
            {
                changedWordIds.Add(wordId);
            }
        }

        // Output the summary
        Console.WriteLine($"\nSummary of Changes:");
        Console.WriteLine($"Added words: {addedWordIds.Count}");
        Console.WriteLine($"Removed words: {removedWordIds.Count}");
        Console.WriteLine($"Changed words: {changedWordIds.Count}");

        // Detailed breakdown of changes
        Console.WriteLine($"\nDetailed Changes:");
        Console.WriteLine($"Words with reading changes: {readingChanges.Count}");
        Console.WriteLine($"Words with parts of speech changes: {posChanges.Count}");
        Console.WriteLine($"Words with priority changes: {priorityChanges.Count}");

        // List removed words
        Console.WriteLine($"\nRemoved Words:");
        foreach (var wordId in removedWordIds)
        {
            var word = oldWordDict[wordId];
            Console.WriteLine($"  WordId: {wordId}, Readings: {string.Join(", ", word.Readings)}");
        }
    }

    private static void MergeNameEntries(JmDictWord target, JmDictWord source)
    {
        // Merge readings (avoiding duplicates)
        foreach (var reading in source.Readings)
        {
            if (!target.Readings.Contains(reading))
            {
                target.Readings.Add(reading);
                target.ReadingTypes.Add(JmDictReadingType.Reading);
            }
        }

        // Merge definitions
        target.Definitions.AddRange(source.Definitions);

        // Merge priorities
        if (source.Priorities != null && source.Priorities.Count > 0)
        {
            if (target.Priorities == null)
                target.Priorities = new List<string>();

            foreach (var priority in source.Priorities)
            {
                if (!target.Priorities.Contains(priority))
                    target.Priorities.Add(priority);
            }
        }
    }

    private static async Task<List<JmDictWord>> GetWordInfos(string dtdPath, string dictionaryPath)
    {
        Regex reg = new Regex(@"<!ENTITY (.*) ""(.*)"">");

        var dtdLines = await File.ReadAllLinesAsync(dtdPath);
        dtdLines = dtdLines.Concat([
            "<!ENTITY name-char \"character\">", "<!ENTITY name-company \"company name\">",
            "<!ENTITY name-creat \"creature\">", "<!ENTITY name-dei \"deity\">",
            "<!ENTITY name-doc \"document\">", "<!ENTITY name-ev \"event\">",
            "<!ENTITY name-fem \"female given name or forename\">", "<!ENTITY name-fict \"fiction\">",
            "<!ENTITY name-given \"given name or forename, gender not specified\">",
            "<!ENTITY name-group \"group\">", "<!ENTITY name-leg \"legend\">",
            "<!ENTITY name-masc \"male given name or forename\">", "<!ENTITY name-myth \"mythology\">",
            "<!ENTITY name-obj \"object\">", "<!ENTITY name-organization \"organization name\">",
            "<!ENTITY name-oth \"other\">", "<!ENTITY name-person \"full name of a particular person\">",
            "<!ENTITY name-place \"place name\">", "<!ENTITY name-product \"product name\">",
            "<!ENTITY name-relig \"religion\">", "<!ENTITY name-serv \"service\">",
            "<!ENTITY name-ship \"ship name\">", "<!ENTITY name-station \"railway station\">",
            "<!ENTITY name-surname \"family or surname\">", "<!ENTITY name-unclass \"unclassified name\">",
            "<!ENTITY name-work \"work of art, literature, music, etc. name\">"
        ]).ToArray();

        foreach (var line in dtdLines)
        {
            var matches = reg.Match(line);
            if (matches.Length > 0 && !_entities.ContainsKey(matches.Groups[1].Value))
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

        return wordInfos;
    }

    private static async Task<JmDictWord> ParseNameKEle(XmlReader reader, JmDictWord wordInfo)
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

    private static async Task<JmDictWord> ParseNameREle(XmlReader reader, JmDictWord wordInfo)
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

    private static async Task<JmDictWord> ParseNameTrans(XmlReader reader, JmDictWord wordInfo)
    {
        if (reader.Name != "trans") return wordInfo;

        var definition = new JmDictDefinition();

        while (await reader.ReadAsync())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                if (reader.Name == "name_type")
                {
                    var nameType = await reader.ReadElementContentAsStringAsync();
                    definition.PartsOfSpeech.Add(ElToPos(nameType));
                }

                if (reader.Name == "trans_det")
                    definition.EnglishMeanings.Add(await reader.ReadElementContentAsStringAsync());
            }

            if (reader.NodeType != XmlNodeType.EndElement) continue;
            if (reader.Name != "trans") continue;

            // Add a general "name" part of speech if no specific type was provided
            if (definition.PartsOfSpeech.Count == 0)
                definition.PartsOfSpeech.Add("name");

            // Add the definition only if it has translations
            if (definition.EnglishMeanings.Count > 0 ||
                definition.DutchMeanings.Count > 0 ||
                definition.FrenchMeanings.Count > 0 ||
                definition.GermanMeanings.Count > 0 ||
                definition.SpanishMeanings.Count > 0 ||
                definition.HungarianMeanings.Count > 0 ||
                definition.RussianMeanings.Count > 0 ||
                definition.SlovenianMeanings.Count > 0)
            {
                wordInfo.Definitions.Add(definition);
            }

            break;
        }

        return wordInfo;
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
                                WordId = 8000000, Readings = ["でした"], ReadingsFurigana = ["でした"],
                                ReadingTypes = [JmDictReadingType.KanaReading], Definitions =
                                [
                                    new JmDictDefinition { EnglishMeanings = ["was, were"], PartsOfSpeech = ["exp"] }
                                ]
                            });

        customWordInfos.Add(new JmDictWord
                            {
                                WordId = 8000001, Readings = ["イクシオトキシン"], ReadingsFurigana = ["イクシオトキシン"],
                                ReadingTypes = [JmDictReadingType.KanaReading], Definitions =
                                [
                                    new JmDictDefinition { EnglishMeanings = ["ichthyotoxin"], PartsOfSpeech = ["n"] }
                                ]
                            });

        customWordInfos.Add(new JmDictWord
                            {
                                WordId = 8000002, Readings = ["逢魔", "おうま"], ReadingsFurigana = ["逢[おう]魔[ま]", "おうま"],
                                ReadingTypes = [JmDictReadingType.Reading, JmDictReadingType.KanaReading], 
                                PitchAccents = [0],
                                Definitions =
                                [
                                    new JmDictDefinition
                                    {
                                        EnglishMeanings =
                                        [
                                            "meeting with evil spirits; encounter with demons or monsters",
                                            "(esp. in compounds) reference to the supernatural or ominous happenings at twilight (逢魔が時 \"the time to meet demons\")"
                                        ],
                                        PartsOfSpeech = ["exp"]
                                    }
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

    public static async Task<bool> ImportVocabularyOrigin(bool verbose, DbContextOptions<JitenDbContext> options,
                                                          string vocabularyOriginFilePath)
    {
        if (!File.Exists(vocabularyOriginFilePath))
        {
            Console.WriteLine($"File {vocabularyOriginFilePath} does not exist.");
            return false;
        }

        var wordOriginMap = new Dictionary<string, WordOrigin>();

        using (var reader = new StreamReader(vocabularyOriginFilePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var anonymousTypeDefinition = new { word = string.Empty, origin = string.Empty };
            var records = csv.GetRecords(anonymousTypeDefinition);

            foreach (var record in records)
            {
                WordOrigin origin = WordOrigin.Unknown;

                switch (record.origin.Trim().ToLowerInvariant())
                {
                    case "和":
                        origin = WordOrigin.Wago;
                        break;
                    case "漢":
                        origin = WordOrigin.Kango;
                        break;
                    case "外":
                        origin = WordOrigin.Gairaigo;
                        break;
                }

                wordOriginMap[record.word] = origin;
            }
        }

        if (verbose)
            Console.WriteLine($"Loaded {wordOriginMap.Count} word origins from CSV file");

        var context = new JitenDbContext(options);
        var jmdictWords = await context.JMDictWords.ToListAsync();
        int updatedCount = 0;

        foreach (var word in jmdictWords)
        {
            string? matchedReading = null;

            // Try kanji readings first
            foreach (var reading in word.Readings)
            {
                if (!wordOriginMap.ContainsKey(reading) ||
                    word.ReadingTypes[word.Readings.IndexOf(reading)] != JmDictReadingType.Reading) continue;
                matchedReading = reading;
                break;
            }

            // If no kanji reading matched, try kana readings
            if (matchedReading == null)
            {
                foreach (var reading in word.Readings)
                {
                    if (!wordOriginMap.ContainsKey(reading)) continue;

                    matchedReading = reading;
                    break;
                }
            }

            if (matchedReading == null) continue;

            word.Origin = wordOriginMap[matchedReading];
            updatedCount++;

            if (verbose && updatedCount % 1000 == 0)
                Console.WriteLine($"Updated {updatedCount} words so far");
        }

        if (verbose)
            Console.WriteLine($"Updated origins for {updatedCount} words. Saving changes to database...");

        await context.SaveChangesAsync();

        return true;
    }
}