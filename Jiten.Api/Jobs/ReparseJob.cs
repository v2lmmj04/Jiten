using Jiten.Core;
using Jiten.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Jiten.Api.Jobs;

public class ReparseJob(JitenDbContext context)
{
    public async Task Reparse(int deckId)
    {
        var deck = await context.Decks.AsNoTracking().Include(d => d.RawText).Include(d => d.Children).ThenInclude(deck => deck.RawText)
                                .FirstOrDefaultAsync(d => d.DeckId == deckId);
        if (deck == null)
            throw new Exception($"Deck with ID {deckId} not found.");

        if (deck.RawText == null && deck.Children.Count == 0)
            throw new Exception($"Deck with ID {deckId} has no raw text to reparse.");

        var children = deck.Children.ToList();

        if (deck.Children.Count == 0)
        {
            Deck newDeck = await Parser.Program.ParseTextToDeck(context, deck.RawText.RawText, true);
            deck.CharacterCount = newDeck.CharacterCount;
            deck.WordCount = newDeck.WordCount;
            deck.UniqueWordCount = newDeck.UniqueWordCount;
            deck.UniqueWordUsedOnceCount = newDeck.UniqueWordUsedOnceCount;
            deck.UniqueKanjiCount = newDeck.UniqueKanjiCount;
            deck.UniqueKanjiUsedOnceCount = newDeck.UniqueKanjiUsedOnceCount;
            deck.SentenceCount = newDeck.SentenceCount;
            deck.DeckWords = newDeck.DeckWords;
            
            if (deck.MediaType is MediaType.Manga or MediaType.Anime or MediaType.Movie or MediaType.Drama)
                deck.SentenceCount = 0;
        }
        else
        {
            for (int i = 0; i < children.Count; i++)
            {
                var child = children.ElementAt(i);
                if (child.RawText == null)
                    throw new Exception($"Child deck with ID {child.DeckId} has no raw text to reparse.");

                Deck newDeck =  await Parser.Program.ParseTextToDeck(context, child.RawText.RawText, true);
                
                children[i].CharacterCount = newDeck.CharacterCount;
                children[i].WordCount = newDeck.WordCount;
                children[i].UniqueWordCount = newDeck.UniqueWordCount;
                children[i].UniqueWordUsedOnceCount = newDeck.UniqueWordUsedOnceCount;
                children[i].UniqueKanjiCount = newDeck.UniqueKanjiCount;
                children[i].UniqueKanjiUsedOnceCount = newDeck.UniqueKanjiUsedOnceCount;
                children[i].SentenceCount = newDeck.SentenceCount;
                children[i].DeckWords = newDeck.DeckWords;
                
                if (children[i].MediaType is MediaType.Manga or MediaType.Anime or MediaType.Movie or MediaType.Drama)
                    children[i].SentenceCount = 0;
            }

            deck.Children = children;
            await deck.AddChildDeckWords(context);
        }
        
        deck.LastUpdate = DateTime.UtcNow;
        
        await JitenHelper.InsertDeck(context.DbOptions, deck, [], true);
    }
}
