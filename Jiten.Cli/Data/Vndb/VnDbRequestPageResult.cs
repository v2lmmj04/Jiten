namespace Jiten.Cli.Data.Vndb;

public class VnDbRequestPageResult
{
    public List<VndbRequestResult> Results { get; set; } = new();
    public bool More { get; set; }
}