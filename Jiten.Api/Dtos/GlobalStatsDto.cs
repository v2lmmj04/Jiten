using Jiten.Core.Data;

namespace Jiten.Api.Dtos;

public class GlobalStatsDto
{
    public Dictionary<MediaType, int> MediaByType { get; set; } = new();
    public int TotalMedia { get; set; }
    public float TotalMojis { get; set; }
}