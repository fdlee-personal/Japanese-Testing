using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JPracticeWeb.Models;
using JPracticeWeb.Services;

namespace JPracticeWeb.Pages;

public class AddWordModel : PageModel
{
    private readonly IJapaneseAutoFillService _autoFillService;

    public AddWordModel(IJapaneseAutoFillService autoFillService)
    {
        _autoFillService = autoFillService;
    }

    [BindProperty]
    public TestClass Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public string? AutoFillMessage { get; private set; }
    public bool AutoFillSucceeded { get; private set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        FillMissingKanaFromTerms();
        ModelState.ClearValidationState(nameof(Input));
        if (!TryValidateModel(Input, nameof(Input)))
        {
            return Page();
        }

        var normalizedKorean = Input.KoreanWord.Trim();
        var normalizedDictionary = Input.DictionaryTerm.Trim();
        var isDuplicate = TestWordStore.GetAll().Any(x =>
            string.Equals(x.KoreanWord, normalizedKorean, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.DictionaryTerm, normalizedDictionary, StringComparison.Ordinal));

        if (isDuplicate)
        {
            ModelState.AddModelError(string.Empty, "같은 단어가 이미 등록되어 있습니다. (단어 + 사전형 중복)");
            return Page();
        }

        TestWordStore.Add(new TestClass
        {
            KoreanWord = normalizedKorean,
            DictionaryTerm = normalizedDictionary,
            DictionaryTermKana = Input.DictionaryTermKana.Trim(),
            PoliteForm = Input.PoliteForm.Trim(),
            PoliteFormKana = Input.PoliteFormKana.Trim(),
            NegativeForm = Input.NegativeForm.Trim(),
            NegativeFormKana = Input.NegativeFormKana.Trim(),
            NegativePoliteForm = Input.NegativePoliteForm.Trim(),
            NegativePoliteFormKana = Input.NegativePoliteFormKana.Trim(),
            PastForm = Input.PastForm.Trim(),
            PastFormKana = Input.PastFormKana.Trim(),
            PastPoliteForm = Input.PastPoliteForm.Trim(),
            PastPoliteFormKana = Input.PastPoliteFormKana.Trim(),
            PastNegativeForm = Input.PastNegativeForm.Trim(),
            PastNegativeFormKana = Input.PastNegativeFormKana.Trim(),
            PastNegativePoliteForm = Input.PastNegativePoliteForm.Trim(),
            PastNegativePoliteFormKana = Input.PastNegativePoliteFormKana.Trim(),
            ConnectiveForm = Input.ConnectiveForm.Trim(),
            ConnectiveFormKana = Input.ConnectiveFormKana.Trim()
        });

        TempData["WordListMessage"] = $"Word saved: {Input.KoreanWord.Trim()}";
        return RedirectToPage("/WordList", new { p = 1 });
    }

    public async Task<IActionResult> OnPostAutoFillAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Input.KoreanWord))
        {
            ModelState.AddModelError("Input.KoreanWord", "Enter a Korean word before auto-fill.");
            return Page();
        }

        var result = await _autoFillService.AutoFillFromKoreanAsync(Input.KoreanWord, cancellationToken);
        AutoFillMessage = result.Message;
        AutoFillSucceeded = result.Success;

        if (!result.Success || result.FilledWord is null)
        {
            return Page();
        }

        Input = result.FilledWord;
        Input.Id = 0;
        AutoFillMessage = "Auto-fill complete. Review the fields, then click Save Word to store it.";

        ModelState.Clear();
        return Page();
    }

    public async Task<IActionResult> OnPostSearchAsync(CancellationToken cancellationToken)
    {
        var hasKorean = !string.IsNullOrWhiteSpace(Input.KoreanWord);
        var hasJapaneseDictionary = !string.IsNullOrWhiteSpace(Input.DictionaryTerm);

        if (!hasKorean && !hasJapaneseDictionary)
        {
            ModelState.AddModelError(string.Empty, "단어 또는 사전형 중 하나는 입력하세요.");
            return Page();
        }

        // If both are filled, prefer Japanese dictionary form as requested.
        if (hasJapaneseDictionary)
        {
            return await OnPostAutoFillFromJapaneseAsync(cancellationToken);
        }

        return await OnPostAutoFillAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAutoFillFromJapaneseAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Input.DictionaryTerm))
        {
            ModelState.AddModelError("Input.DictionaryTerm", "사전형(일본어)을 먼저 입력하세요.");
            return Page();
        }

        var result = await _autoFillService.AutoFillFromJapaneseDictionaryAsync(Input.DictionaryTerm, Input.KoreanWord, cancellationToken);
        AutoFillMessage = result.Message;
        AutoFillSucceeded = result.Success;

        if (!result.Success || result.FilledWord is null)
        {
            return Page();
        }

        var existingKorean = Input.KoreanWord;
        Input = result.FilledWord;
        Input.Id = 0;
        if (!string.IsNullOrWhiteSpace(existingKorean))
        {
            Input.KoreanWord = existingKorean.Trim();
        }

        ModelState.Clear();
        return Page();
    }

    private void FillMissingKanaFromTerms()
    {
        Input.KoreanWord = Input.KoreanWord?.Trim() ?? string.Empty;
        Input.DictionaryTerm = Input.DictionaryTerm?.Trim() ?? string.Empty;
        Input.PoliteForm = Input.PoliteForm?.Trim() ?? string.Empty;
        Input.NegativeForm = Input.NegativeForm?.Trim() ?? string.Empty;
        Input.NegativePoliteForm = Input.NegativePoliteForm?.Trim() ?? string.Empty;
        Input.PastForm = Input.PastForm?.Trim() ?? string.Empty;
        Input.PastPoliteForm = Input.PastPoliteForm?.Trim() ?? string.Empty;
        Input.PastNegativeForm = Input.PastNegativeForm?.Trim() ?? string.Empty;
        Input.PastNegativePoliteForm = Input.PastNegativePoliteForm?.Trim() ?? string.Empty;
        Input.ConnectiveForm = Input.ConnectiveForm?.Trim() ?? string.Empty;

        Input.DictionaryTermKana = GetKanaOrFallback(Input.DictionaryTermKana, Input.DictionaryTerm);
        Input.PoliteFormKana = GetKanaOrFallback(Input.PoliteFormKana, Input.PoliteForm);
        Input.NegativeFormKana = GetKanaOrFallback(Input.NegativeFormKana, Input.NegativeForm);
        Input.NegativePoliteFormKana = GetKanaOrFallback(Input.NegativePoliteFormKana, Input.NegativePoliteForm);
        Input.PastFormKana = GetKanaOrFallback(Input.PastFormKana, Input.PastForm);
        Input.PastPoliteFormKana = GetKanaOrFallback(Input.PastPoliteFormKana, Input.PastPoliteForm);
        Input.PastNegativeFormKana = GetKanaOrFallback(Input.PastNegativeFormKana, Input.PastNegativeForm);
        Input.PastNegativePoliteFormKana = GetKanaOrFallback(Input.PastNegativePoliteFormKana, Input.PastNegativePoliteForm);
        Input.ConnectiveFormKana = GetKanaOrFallback(Input.ConnectiveFormKana, Input.ConnectiveForm);
    }

    private static string GetKanaOrFallback(string? kana, string fallbackTerm)
    {
        return string.IsNullOrWhiteSpace(kana) ? fallbackTerm : kana.Trim();
    }
}
