namespace Jiten.Core.Data;

public class Deck
{
    
    /// <summary>
    /// Autoincrement id, starting at 1
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Type of media the deck belongs to
    /// </summary>
    public MediaType MediaType { get; set; } = new();

    /// <summary>
    /// Original title of the work, generally in kanji
    /// </summary>
    public string OriginalTitle { get; set; } = "Unknown";

    /// <summary>
    /// Romaji transcription of the title
    /// </summary>
    public string? RomajiTitle { get; set; }

    /// <summary>
    /// English translation of the title, if it exists
    /// </summary>
    public string? EnglishTitle { get; set; }
    
    /// <summary>
    /// Total character count, without punctuation
    /// </summary>
    public int CharacterCount { get; set; }
    
    /// <summary>
    /// Total word count, non-unique
    /// </summary>
    public int WordCount { get; set; }
    
    /// <summary>
    /// Total unique word count after deconjugation
    /// </summary>
    public int UniqueWordCount { get; set; }
    
    /// <summary>
    /// Total unique word count after deconjugation, used only once
    /// </summary>
    public int UniqueWordUsedOnceCount { get; set; }
    
    /// <summary>
    /// Total unique kanji count
    /// </summary>
    public int UniqueKanjiCount { get; set; } // Unique Kanji count
    
    /// <summary>
    /// Total unique kanji count, used only once
    /// </summary>
    public int UniqueKanjiUsedOnceCount { get; set; }
    
    /// <summary>
    ///  Difficulty rating from 0 to 100
    /// </summary>
    public int Difficulty { get; set; }
    
    /// <summary>
    /// Average sentence length with decimal precision
    /// </summary>
    public float  AverageSentenceLength { get; set; } 
    
    /// <summary>
    /// Parent deck, 0 if no parent
    /// </summary>
    public int ParentDeckId { get; set; }

    public ICollection<DeckWord> DeckWords { get; set; }
    
    public List<Link> Links { get; set; }

}
