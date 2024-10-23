namespace Jiten.Core.Data;

public class Link
{
        public int LinkId { get; set; }
        public LinkType LinkType { get; set; }
        public string Url { get; set; }
        public int DeckId { get; set; }
        public Deck Deck { get; set; }
}