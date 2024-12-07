namespace Jiten.Api.Dtos;

public class DeckVocabularyListDto
{
    public int DeckId { get; set; }
    public string Title { get; set; } = "";
    public List<WordDto> Words { get; set; }
}