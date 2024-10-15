using System.Text.Json.Serialization;

namespace JapaneseParser.DictionaryTools;

public class JMDictWordInfo
{
    public List<string> Readings { get; set; }

    public List<string> KanaReading { get; set; }

    public int EntrySequenceId { get; set; }

    public List<Definition> Definitions { get; set; }

    public JMDictWordInfo()
    {
        Readings = new List<string>();
        KanaReading = new List<string>();
        Definitions = new List<Definition>();
    }
}

public class Definition
{
    public List<string> EnglishMeanings { get; set; }

    public List<string> DutchMeanings { get; set; }

    public List<string> FrenchMeanings { get; set; }

    public List<string> GermanMeanings { get; set; }

    public List<string> SpanishMeanings { get; set; }

    public List<string> HungarianMeanings { get; set; }

    public List<string> RussianMeanings { get; set; }

    public List<string> SlovenianMeanings { get; set; }

    public List<string> PartsOfSpeech { get; set; }

    public Definition()
    {
        PartsOfSpeech = new List<string>();
        EnglishMeanings = new List<string>();
        DutchMeanings = new List<string>();
        FrenchMeanings = new List<string>();
        GermanMeanings = new List<string>();
        SpanishMeanings = new List<string>();
        HungarianMeanings = new List<string>();
        RussianMeanings = new List<string>();
        SlovenianMeanings = new List<string>();
    }
}