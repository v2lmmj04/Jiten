using Jiten.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Jiten.Core;

public static class JitenHelper
{
    public static async Task InsertDeck(Deck deck)
    {
        // Ignore if the deck already exists
        await using var context = new JitenDbContext();

        if (await context.Decks.AnyAsync(d => d.OriginalTitle == deck.OriginalTitle))
            return;

        // Fix potential null references to decks
        deck.SetParentsAndDeckWordDeck(deck);
        deck.ParentDeckId = null;

        context.Decks.Add(deck);

        await context.SaveChangesAsync();
    }
}