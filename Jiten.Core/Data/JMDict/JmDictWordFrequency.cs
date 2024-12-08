namespace Jiten.Core.Data.JMDict;

public class JmDictWordFrequency
{
    public int WordId { get; set; }
    public int FrequencyRank { get; set; }
    public int UsedInMediaAmount { get; set; }
    public double ObservedFrequency { get; set; }
    public List<int> ReadingsFrequencyRank { get; set; } = new();
    public List<double> ReadingsFrequencyPercentage { get; set; } = new();
    public List<double> ReadingsObservedFrequency { get; set; } = new();
    public List<int> ReadingsUsedInMediaAmount { get; set; } = new();
}