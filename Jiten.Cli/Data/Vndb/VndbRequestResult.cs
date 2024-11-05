namespace VndbRecommender.Model;

public class VndbRequestResult
{
    public string? Id { get; set; }
    public string Title { get; set; }
    public List<VnTitle> Titles { get; set; }
    public VnImage Image { get; set; }
}