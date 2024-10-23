namespace Jiten.Core.Data;

public class DeckWord
{
    // Corresponding deck id
    public int DeckId { get; set; }
    
    /// <summary>
    /// Corresponding word id
    /// </summary>
    public int WordId { get; set; }
    
    /// <summary>
    /// 0 for reading, 1 for kanareading
    /// </summary>
    public byte ReadingType { get; set; }
    
    /// <summary>
    /// The index of the reading in the list of readings
    /// </summary>
    public byte ReadingIndex { get; set; }
    
    /// <summary>
    /// Number of times the exact word & reading appears in the deck
    /// </summary>
    public int Occurrences { get; set; }

    public Deck Deck { get; set; }
}
