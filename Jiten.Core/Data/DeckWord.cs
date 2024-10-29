namespace Jiten.Core.Data;

public class DeckWord
{
    public int Id { get; set; }
    
    /// <summary>
    /// Corresponding deck id
    /// </summary>
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
