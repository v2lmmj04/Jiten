namespace Jiten.Parser;

public class DeconjugationForm
{
    public List<string> Tags { get; }
    public string Text { get; }
    public string OriginalText { get; }
    public HashSet<string> SeenText { get; }
    public List<String> Process { get; }

    public DeconjugationForm(string text, string originalText, List<string> tags, HashSet<string> seenText, List<string> process)
    {
        Text = text;
        OriginalText = originalText;
        Tags = tags;
        SeenText = seenText;
        Process = process;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        DeconjugationForm other = (DeconjugationForm)obj;
        return Text == other.Text &&
               OriginalText == other.OriginalText &&
               Tags.SequenceEqual(other.Tags) &&
               SeenText.SetEquals(other.SeenText) &&
               Process.SequenceEqual(other.Process);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Text, StringComparer.Ordinal);
        hash.Add(OriginalText, StringComparer.Ordinal);
        foreach (var tag in Tags)
        {
            hash.Add(tag, StringComparer.Ordinal);
        }

        foreach (var process in Process)
        {
            hash.Add(process, StringComparer.Ordinal);
        }

        foreach (var seenText in SeenText)
        {
            hash.Add(seenText, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}