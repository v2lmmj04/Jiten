namespace Jiten.Parser;


public enum PartOfSpeech
{
    Noun = 1,
    Verb = 2,
    IAdjective = 3,
    Adverb = 4,
    Particle = 5,
    Conjunction = 6,
    Auxiliary = 7,
    Adnominal = 8,
    Interjection = 9,
    Symbol = 10,
    Prefix = 11,
    Filler = 12,
    Name = 13,
    Pronoun = 14,
    NaAdjective = 15,
    Suffix = 16,
    CommonNoun = 17,
    SupplementarySymbol = 18,
    BlankSpace = 19
}

public enum PartOfSpeechSection
{
    None = 0,
    Amount = 1,
    Alphabet = 2,
    FullStop = 3,
    BlankSpace = 4,
    Suffix = 5,
    Pronoun = 6,
    Independant = 7,
    Dependant = 8,
    Filler = 9,
    Common = 10,
    SentenceEndingParticle = 11,
    Counter = 12,
    ParallelMarker = 13,
    BindingParticle = 14,
    PotentialAdverb = 15,
    CaseMarkingParticle = 16,
    IrregularConjunction = 17,
    ConjunctionParticle = 18,
    AuxiliaryVerbStem = 19,
    AdjectivalStem = 20,
    CompoundWord = 21,
    Quotation = 22,
    NounConjunction = 23,
    AdverbialParticle = 24,
    ConjunctiveParticleClass = 25,
    Adverbialization = 26,
    AdverbialParticleOrParallelMarkerOrSentenceEndingParticle = 27,
    AdnominalAdjective = 28,
    ProperNoun = 29,
    Special = 30,
    VerbConjunction = 31,
    PersonName = 32,
    FamilyName = 33,
    Organization = 34,
    NotAdjectiveStem = 35,
    Comma = 36,
    OpeningBracket = 37,
    ClosingBracket = 38,
    Region = 39,
    Country = 40,
    Numeral = 41,
    PossibleDependant = 42,
    CommonNoun = 43,
    SubstantiveAdjective = 44,
    PossibleCounterWord = 45,
    PossibleSuru = 46,
    Juntaijoushi = 47,
    PossibleNaAdjective = 48,
    VerbLike = 49,
    PossibleVerbSuruNoun = 50,
    Adjectival = 51,
    NaAdjectiveLike = 52,
    Name = 53,
    Letter = 54,
    PlaceName = 55,
    TaruAdjective = 56
}

public static class PartOfSpeechExtension
{
    public static PartOfSpeech ToPartOfSpeech(this string pos)
    {
        switch (pos)
        {
            case "名詞":
                return PartOfSpeech.Noun;
            case "動詞":
                return PartOfSpeech.Verb;
            case "形容詞":
                return PartOfSpeech.IAdjective;
            case "形状詞":
                return PartOfSpeech.NaAdjective;
            case "副詞":
                return PartOfSpeech.Adverb;
            case "助詞":
                return PartOfSpeech.Particle;
            case "接続詞":
                return PartOfSpeech.Conjunction;
            case "助動詞":
                return PartOfSpeech.Auxiliary;
            case "連体詞":
                return PartOfSpeech.Adnominal;
            case "感動詞":
                return PartOfSpeech.Interjection;
            case "記号":
                return PartOfSpeech.Symbol;
            case "接頭詞":
            case "接頭辞":
                return PartOfSpeech.Prefix;
            case "フィラー":
                return PartOfSpeech.Filler;
            case "名":
                return PartOfSpeech.Name;
            case "代名詞":
                return PartOfSpeech.Pronoun;
            case "接尾辞":
                return PartOfSpeech.Suffix;
            case "普通名詞":
                return PartOfSpeech.CommonNoun;
            case "補助記号":
                return PartOfSpeech.SupplementarySymbol;
            case "空白":
                return PartOfSpeech.BlankSpace;

            default:
                throw new ArgumentException($"Invalid part of speech : {pos}");
        }
    }

    public static PartOfSpeechSection ToPartOfSpeechSection(this string pos)
    {
        switch (pos)
        {
            case "*":
                return PartOfSpeechSection.None;
            case "数":
                return PartOfSpeechSection.Amount;
            case "アルファベット":
                return PartOfSpeechSection.Alphabet;
            case "句点":
                return PartOfSpeechSection.FullStop;
            case "空白":
                return PartOfSpeechSection.BlankSpace;
            case "接尾":
                return PartOfSpeechSection.Suffix;
            case "代名詞":
                return PartOfSpeechSection.Pronoun;
            case "自立":
                return PartOfSpeechSection.Independant;
            case "フィラー":
                return PartOfSpeechSection.Filler;
            case "一般":
                return PartOfSpeechSection.Common;
            case "非自立":
                return PartOfSpeechSection.Dependant;
            case "終助詞":
                return PartOfSpeechSection.SentenceEndingParticle;
            case "助数詞":
                return PartOfSpeechSection.Counter;
            case "並立助詞":
                return PartOfSpeechSection.ParallelMarker;
            case "係助詞":
                return PartOfSpeechSection.BindingParticle;
            case "副詞可能":
                return PartOfSpeechSection.PotentialAdverb;
            case "格助詞":
                return PartOfSpeechSection.CaseMarkingParticle;
            case "サ変接続":
                return PartOfSpeechSection.IrregularConjunction;
            case "接続助詞":
                return PartOfSpeechSection.ConjunctionParticle;
            case "助動詞語幹":
                return PartOfSpeechSection.AuxiliaryVerbStem;
            case "形容動詞語幹":
                return PartOfSpeechSection.AdjectivalStem;
            case "連語":
                return PartOfSpeechSection.CompoundWord;
            case "引用":
                return PartOfSpeechSection.Quotation;
            case "名詞接続":
                return PartOfSpeechSection.NounConjunction;
            case "副助詞":
                return PartOfSpeechSection.AdverbialParticle;
            case "助詞類接続":
                return PartOfSpeechSection.ConjunctiveParticleClass;
            case "副詞化":
                return PartOfSpeechSection.Adverbialization;
            case "副助詞／並立助詞／終助詞":
                return PartOfSpeechSection.AdverbialParticleOrParallelMarkerOrSentenceEndingParticle;
            case "連体化":
                return PartOfSpeechSection.AdnominalAdjective;
            case "固有名詞":
                return PartOfSpeechSection.ProperNoun;
            case "特殊":
                return PartOfSpeechSection.Special;
            case "動詞接続":
                return PartOfSpeechSection.VerbConjunction;
            case "人名":
                return PartOfSpeechSection.PersonName;
            case "姓":
                return PartOfSpeechSection.FamilyName;
            case "組織":
                return PartOfSpeechSection.Organization;
            case "ナイ形容詞語幹":
                return PartOfSpeechSection.NotAdjectiveStem;
            case "読点":
                return PartOfSpeechSection.Comma;
            case "括弧開":
                return PartOfSpeechSection.OpeningBracket;
            case "括弧閉":
                return PartOfSpeechSection.ClosingBracket;
            case "地域":
                return PartOfSpeechSection.Region;
            case "国":
                return PartOfSpeechSection.Country;
            case "数詞":
                return PartOfSpeechSection.Numeral;
            case "非自立可能":
                return PartOfSpeechSection.PossibleDependant;
            case "普通名詞":
                return PartOfSpeechSection.CommonNoun;
            case "名詞的":
                return PartOfSpeechSection.SubstantiveAdjective;
            case "助数詞可能":
                return PartOfSpeechSection.PossibleCounterWord;
            case "サ変可能":
                return PartOfSpeechSection.PossibleSuru;
            case "準体助詞":
                return PartOfSpeechSection.Juntaijoushi;
            case "形状詞可能":
                return PartOfSpeechSection.PossibleNaAdjective;
            case "動詞的":
                return PartOfSpeechSection.VerbLike;
            case "サ変形状詞可能":
                return PartOfSpeechSection.PossibleVerbSuruNoun;
            case "形容詞的":
                return PartOfSpeechSection.Adjectival;
            case "名":
                return PartOfSpeechSection.Name;
            case "文字":
                return PartOfSpeechSection.Letter;
            case "形状詞的":
                return PartOfSpeechSection.NaAdjectiveLike;
            case "地名":
                return PartOfSpeechSection.PlaceName;
            case "タリ":
                return PartOfSpeechSection.TaruAdjective;

            default:
                throw new ArgumentException($"Invalid part of speech section : {pos}");
        }
    }
}