using Jiten.Core.Data;

namespace Jiten.Api.Dtos.Requests;

public class UpdateMediaRequest
{
    public bool Reparse { get; set; }
    public int DeckId { get; set; }
    public DateOnly ReleaseDate { get; set; }
    public MediaType MediaType { get; set; }
    public required string OriginalTitle { get; set; }
    public string? RomajiTitle { get; set; }
    public string? EnglishTitle { get; set; }
    public string? Description { get; set; }
    public float DifficultyOverride { get; set; }
    public bool HideDialoguePercentage { get; set; }
    public IFormFile? CoverImage { get; set; }
    public IFormFile? File { get; set; }
    public List<Link> Links { get; set; } = new List<Link>();
    public List<string> Aliases { get; set; } = new List<string>();
    public List<UpdateMediaRequestSubdeck>? Subdecks { get; set; } = new List<UpdateMediaRequestSubdeck>();
}

public class UpdateMediaRequestSubdeck
{
    public required string OriginalTitle { get; set; }
    public int DeckId { get; set; }
    public int DeckOrder { get; set; }
    public float DifficultyOverride { get; set; }
    public IFormFile? File { get; set; }
}
