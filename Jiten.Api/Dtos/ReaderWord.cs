using Jiten.Core.Data.User;

namespace Jiten.Api.Dtos;

public class ReaderWord
{
    public int WordId { get; set; }
    public byte ReadingIndex { get; set; }
    public string Spelling { get; set; } = string.Empty;
    public string Reading { get; set; } = string.Empty;
    public int FrequencyRank { get; set; }
    public List<string> PartsOfSpeech { get; set; } = new();
    public List<List<string>> MeaningsChunks { get; set; } = new();
    public List<string> MeaningsPartOfSpeech { get; set; } = new();
    public KnownState KnownState { get; set; }
    public List<int> PitchAccents { get; set; } = new();
}