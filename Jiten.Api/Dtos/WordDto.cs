namespace Jiten.Api.Dtos;

public class WordDto
{
    public int WordId { get; set; }
    public string Reading { get; set; } = "";
    public string? VerbReading { get; set; }
    public List<string> AlternativeReadings { get; set; } = new();
    public List<string> PartsOfSpeech { get; set; } = new();
    public List<DefinitionDto> Definitions { get; set; } = new();
}