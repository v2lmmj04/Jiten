namespace Jiten.Core.Data.JMDict;

public class JmDictWordFrequency
{
    public int WordId { get; set; }
    public int FrequencyRank { get; set; }
    public int UsedInMediaAmount { get; set; }
    public List<int> ReadingsFrequencyRank { get; set; } = new();
    public List<double> ReadingsFrequencyPercentage { get; set; } = new();
    public List<int> ReadingsUsedInMediaAmount { get; set; } = new();
    public List<int> KanaReadingsFrequencyRank { get; set; } = new();
    public List<double> KanaReadingsFrequencyPercentage { get; set; } = new();
    public List<int> KanaReadingsUsedInMediaAmount { get; set; } = new();
}