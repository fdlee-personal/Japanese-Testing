using System.ComponentModel.DataAnnotations;

namespace JPracticeWeb.Models;

public class TestClass
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "단어")]
    public string KoreanWord { get; set; } = string.Empty;

    [Required]
    [Display(Name = "사전형")]
    public string DictionaryTerm { get; set; } = string.Empty;
    [Required]
    [Display(Name = "사전형 (가나)")]
    public string DictionaryTermKana { get; set; } = string.Empty;

    [Required]
    [Display(Name = "정중형")]
    public string PoliteForm { get; set; } = string.Empty;
    [Required]
    [Display(Name = "정중형 (가나)")]
    public string PoliteFormKana { get; set; } = string.Empty;

    [Required]
    [Display(Name = "부정형")]
    public string NegativeForm { get; set; } = string.Empty;
    [Required]
    [Display(Name = "부정형 (가나)")]
    public string NegativeFormKana { get; set; } = string.Empty;

    [Required]
    [Display(Name = "부정정중형")]
    public string NegativePoliteForm { get; set; } = string.Empty;
    [Required]
    [Display(Name = "부정정중형 (가나)")]
    public string NegativePoliteFormKana { get; set; } = string.Empty;

    [Required]
    [Display(Name = "과거형")]
    public string PastForm { get; set; } = string.Empty;
    [Required]
    [Display(Name = "과거형 (가나)")]
    public string PastFormKana { get; set; } = string.Empty;

    [Required]
    [Display(Name = "과거정중형")]
    public string PastPoliteForm { get; set; } = string.Empty;
    [Required]
    [Display(Name = "과거정중형 (가나)")]
    public string PastPoliteFormKana { get; set; } = string.Empty;

    [Required]
    [Display(Name = "과거부정형")]
    public string PastNegativeForm { get; set; } = string.Empty;
    [Required]
    [Display(Name = "과거부정형 (가나)")]
    public string PastNegativeFormKana { get; set; } = string.Empty;

    [Required]
    [Display(Name = "과거부정정중형")]
    public string PastNegativePoliteForm { get; set; } = string.Empty;
    [Required]
    [Display(Name = "과거부정정중형 (가나)")]
    public string PastNegativePoliteFormKana { get; set; } = string.Empty;

    [Required]
    [Display(Name = "연결형")]
    public string ConnectiveForm { get; set; } = string.Empty;
    [Required]
    [Display(Name = "연결형 (가나)")]
    public string ConnectiveFormKana { get; set; } = string.Empty;

    public IReadOnlyList<(string Label, string Term, string Kana)> GetJapaneseTerms()
    {
        return
        [
            ("사전형", DictionaryTerm, DictionaryTermKana),
            ("정중형", PoliteForm, PoliteFormKana),
            ("부정형", NegativeForm, NegativeFormKana),
            ("부정정중형", NegativePoliteForm, NegativePoliteFormKana),
            ("과거형", PastForm, PastFormKana),
            ("과거정중형", PastPoliteForm, PastPoliteFormKana),
            ("과거부정형", PastNegativeForm, PastNegativeFormKana),
            ("과거부정정중형", PastNegativePoliteForm, PastNegativePoliteFormKana),
            ("연결형", ConnectiveForm, ConnectiveFormKana)
        ];
    }
}
