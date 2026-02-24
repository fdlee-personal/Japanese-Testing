using JPracticeWeb.Models;
using JPracticeWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JPracticeWeb.Pages;

public class EditWordModel : PageModel
{
    private readonly IJapaneseAutoFillService _autoFillService;

    public EditWordModel(IJapaneseAutoFillService autoFillService)
    {
        _autoFillService = autoFillService;
    }

    [BindProperty]
    public TestClass Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? P { get; set; }

    public int ReturnPage => Math.Max(1, P.GetValueOrDefault(1));

    public string? AutoFillMessage { get; private set; }
    public bool AutoFillSucceeded { get; private set; }

    public IActionResult OnGet(int id, int? p)
    {
        P = p;
        var existing = TestWordStore.GetById(id);
        if (existing is null)
        {
            TempData["WordListMessage"] = "수정할 단어를 찾을 수 없습니다.";
            return RedirectToPage("/WordList", new { p = ReturnPage });
        }

        Input = existing;
        return Page();
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
            x.Id != Input.Id &&
            string.Equals(x.KoreanWord, normalizedKorean, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.DictionaryTerm, normalizedDictionary, StringComparison.Ordinal));

        if (isDuplicate)
        {
            ModelState.AddModelError(string.Empty, "같은 단어가 이미 등록되어 있습니다. (단어 + 사전형 중복)");
            return Page();
        }

        var updated = new TestClass
        {
            Id = Input.Id,
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
        };

        if (!TestWordStore.Update(updated))
        {
            ModelState.AddModelError(string.Empty, "저장 중 오류가 발생했습니다. 단어를 다시 확인하세요.");
            return Page();
        }

        TempData["WordListMessage"] = $"단어를 수정했습니다: {updated.KoreanWord}";
        return RedirectToPage("/WordDetail", new { id = updated.Id, p = ReturnPage });
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

        JapaneseAutoFillResult result;
        if (hasJapaneseDictionary)
        {
            result = await _autoFillService.AutoFillFromJapaneseDictionaryAsync(
                Input.DictionaryTerm, Input.KoreanWord, cancellationToken);
        }
        else
        {
            result = await _autoFillService.AutoFillFromKoreanAsync(Input.KoreanWord, cancellationToken);
        }

        AutoFillMessage = result.Message;
        AutoFillSucceeded = result.Success;

        if (!result.Success || result.FilledWord is null)
        {
            return Page();
        }

        var currentId = Input.Id;
        Input = result.FilledWord;
        Input.Id = currentId;

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
