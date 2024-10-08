using System.Text.RegularExpressions;

namespace Jiten.Parser;

public class WordInfo
{
    public string Text { get; set; }
    public PartOfSpeech PartOfSpeech { get; set; }
    public PartOfSpeechSection PartOfSpeechSection1 { get; set; }
    public PartOfSpeechSection PartOfSpeechSection2 { get; set; }
    public PartOfSpeechSection PartOfSpeechSection3 { get; set; }
    public string DictionaryForm { get; set; }
    public string Reading { get; set; }
    public bool IsInvalid { get; set; }
    
    public WordInfo(string sudachiLine)
    {
        var parts = Regex.Split(sudachiLine, @"\s+");

        if (parts.Length < 6)
        {
            IsInvalid = true;
            return;
        }

        var pos = parts[1].Split(",");

        Text = parts[0];
        PartOfSpeech = pos[0].ToPartOfSpeech();
        PartOfSpeechSection1 = pos[1].ToPartOfSpeechSection();
        PartOfSpeechSection2 = pos[2].ToPartOfSpeechSection();
        PartOfSpeechSection3 = pos[3].ToPartOfSpeechSection();
        DictionaryForm = parts[3];
        Reading = parts[5];
    }
    
    public bool HasPartOfSpeechSection(PartOfSpeechSection section)
    {
        return PartOfSpeechSection1 == section || PartOfSpeechSection2 == section || PartOfSpeechSection3 == section;
    }
}