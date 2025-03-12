using Jiten.Core.Data;

namespace Jiten.Api.Dtos;

public class DeckDetailDto
{
    public Deck MainDeck { get; set; } = new();
    public List<Deck> SubDecks { get; set; } = new();
}