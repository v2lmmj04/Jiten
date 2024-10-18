namespace JapaneseParser.DictionaryTools;

public class JmDictWord
{
    public int WordId { get; set; }
    public List<string> Readings { get; set; } = new();
    public List<string> KanaReadings { get; set; } = new();
    public List<string> PartsOfSpeech { get; set; } = new();
    public List<JmDictDefinition> Definitions { get; set; } = new();
    public List<JmDictLookup> Lookups { get; set; } = new();
}