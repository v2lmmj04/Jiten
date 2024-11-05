namespace VndbRecommender.Model;

public class VnDbRequestPageResult
{
    public List<VndbRequestResult> Results { get; set; } = new();
    public bool More { get; set; }
}