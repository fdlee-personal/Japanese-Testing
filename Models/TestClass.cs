using System.ComponentModel.DataAnnotations;

namespace JPracticeWeb.Models;

public class TestClass
{
    public const string PartOfSpeechVerb = "verb";
    public const string PartOfSpeechNoun = "noun";
    public const string PartOfSpeechAdjective = "adjective";
    public const string PartOfSpeechAdverb = "adverb";
    public const string PartOfSpeechExpression = "expression";
    public const string PartOfSpeechOther = "other";

    public int Id { get; set; }

    [Required]
    [Display(Name = "단어")]
    public string KoreanWord { get; set; } = string.Empty;

    [Display(Name = "품사")]
    public string PartOfSpeech { get; set; } = PartOfSpeechVerb;

    [Required]
    [Display(Name = "사전형")]
    public string DictionaryTerm { get; set; } = string.Empty;

    [Display(Name = "사전형 (가나)")]
    public string DictionaryTermKana { get; set; } = string.Empty;

    [Display(Name = "정중형")]
    public string PoliteForm { get; set; } = string.Empty;

    [Display(Name = "정중형 (가나)")]
    public string PoliteFormKana { get; set; } = string.Empty;

    [Display(Name = "부정형")]
    public string NegativeForm { get; set; } = string.Empty;

    [Display(Name = "부정형 (가나)")]
    public string NegativeFormKana { get; set; } = string.Empty;

    [Display(Name = "부정정중형")]
    public string NegativePoliteForm { get; set; } = string.Empty;

    [Display(Name = "부정정중형 (가나)")]
    public string NegativePoliteFormKana { get; set; } = string.Empty;

    [Display(Name = "과거형")]
    public string PastForm { get; set; } = string.Empty;

    [Display(Name = "과거형 (가나)")]
    public string PastFormKana { get; set; } = string.Empty;

    [Display(Name = "과거정중형")]
    public string PastPoliteForm { get; set; } = string.Empty;

    [Display(Name = "과거정중형 (가나)")]
    public string PastPoliteFormKana { get; set; } = string.Empty;

    [Display(Name = "과거부정형")]
    public string PastNegativeForm { get; set; } = string.Empty;

    [Display(Name = "과거부정형 (가나)")]
    public string PastNegativeFormKana { get; set; } = string.Empty;

    [Display(Name = "과거부정정중형")]
    public string PastNegativePoliteForm { get; set; } = string.Empty;

    [Display(Name = "과거부정정중형 (가나)")]
    public string PastNegativePoliteFormKana { get; set; } = string.Empty;

    [Display(Name = "연결형")]
    public string ConnectiveForm { get; set; } = string.Empty;

    [Display(Name = "연결형 (가나)")]
    public string ConnectiveFormKana { get; set; } = string.Empty;

    public IReadOnlyList<(string Label, string Term, string Kana)> GetJapaneseTerms()
    {
        var terms = new List<(string Label, string Term, string Kana)>();

        AddTermIfPresent(terms, "사전형", DictionaryTerm, DictionaryTermKana);
        AddTermIfPresent(terms, "정중형", PoliteForm, PoliteFormKana);
        AddTermIfPresent(terms, "부정형", NegativeForm, NegativeFormKana);
        AddTermIfPresent(terms, "부정정중형", NegativePoliteForm, NegativePoliteFormKana);
        AddTermIfPresent(terms, "과거형", PastForm, PastFormKana);
        AddTermIfPresent(terms, "과거정중형", PastPoliteForm, PastPoliteFormKana);
        AddTermIfPresent(terms, "과거부정형", PastNegativeForm, PastNegativeFormKana);
        AddTermIfPresent(terms, "과거부정정중형", PastNegativePoliteForm, PastNegativePoliteFormKana);
        AddTermIfPresent(terms, "연결형", ConnectiveForm, ConnectiveFormKana);

        return terms;
    }

    public string GetPartOfSpeechLabel()
    {
        return NormalizePartOfSpeech(PartOfSpeech) switch
        {
            PartOfSpeechNoun => "명사",
            PartOfSpeechAdjective => "형용사",
            PartOfSpeechAdverb => "부사",
            PartOfSpeechExpression => "표현",
            PartOfSpeechOther => "기타",
            _ => "동사"
        };
    }

    public bool IsVerb()
    {
        return string.Equals(NormalizePartOfSpeech(PartOfSpeech), PartOfSpeechVerb, StringComparison.Ordinal);
    }

    public static string NormalizePartOfSpeech(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized switch
        {
            PartOfSpeechNoun => PartOfSpeechNoun,
            PartOfSpeechAdjective => PartOfSpeechAdjective,
            PartOfSpeechAdverb => PartOfSpeechAdverb,
            PartOfSpeechExpression => PartOfSpeechExpression,
            PartOfSpeechOther => PartOfSpeechOther,
            _ => PartOfSpeechVerb
        };
    }

    private static void AddTermIfPresent(ICollection<(string Label, string Term, string Kana)> terms, string label, string term, string kana)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return;
        }

        terms.Add((label, term, kana));
    }
}
