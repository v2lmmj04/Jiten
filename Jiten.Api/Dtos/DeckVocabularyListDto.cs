using Jiten.Core.Data;

namespace Jiten.Api.Dtos;

public class DeckVocabularyListDto
{
    public Deck Deck { get; set; }
    public List<WordDto> Words { get; set; }
}