namespace JapaneseParser.DictionaryTools;

public class JmDictDefinition
{
    public int DefinitionId { get; init; }
    public int WordId { get; init; }
    public List<string> PartsOfSpeech { get; init; } = new();
    public List<string> EnglishMeanings { get; init; } = new();
    public List<string> DutchMeanings { get; init; } = new();
    public List<string> FrenchMeanings { get; init; } = new();
    public List<string> GermanMeanings { get; init; } = new();
    public List<string> SpanishMeanings { get; init; } = new();
    public List<string> HungarianMeanings { get; init; } = new();
    public List<string> RussianMeanings { get; init; } = new();
    public List<string> SlovenianMeanings { get; init; } = new();
}