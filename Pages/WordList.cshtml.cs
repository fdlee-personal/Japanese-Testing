using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JPracticeWeb.Models;
using JPracticeWeb.Services;

namespace JPracticeWeb.Pages;

public class WordListModel : PageModel
{
    private const int PageSize = 20;
    public const string AllTypes = "all";

    public IReadOnlyList<TestClass> Words { get; private set; } = [];
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }
    public int TotalCount { get; private set; }

    [BindProperty(SupportsGet = true, Name = "type")]
    public string SelectedType { get; set; } = AllTypes;

    [TempData]
    public string? StatusMessage { get; set; }

    public IActionResult OnGet(int? p)
    {
        SelectedType = NormalizeSelectedType(SelectedType);
        var all = TestWordStore.GetAll()
            .Where(MatchesSelectedType)
            .OrderByDescending(x => x.Id)
            .ToList();
        TotalCount = all.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

        var requested = p.GetValueOrDefault(1);
        if (requested < 1)
        {
            requested = 1;
        }
        if (requested > TotalPages)
        {
            requested = TotalPages;
        }

        CurrentPage = requested;
        Words = all.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();

        return Page();
    }

    public IActionResult OnPostDelete(int id, int? p)
    {
        var removed = TestWordStore.RemoveById(id);
        StatusMessage = removed ? "단어를 삭제했습니다." : "삭제할 단어를 찾을 수 없습니다.";
        return RedirectToPage(new { p = Math.Max(1, p.GetValueOrDefault(1)), type = NormalizeSelectedType(SelectedType) });
    }

    private bool MatchesSelectedType(TestClass word)
    {
        return string.Equals(SelectedType, AllTypes, StringComparison.Ordinal) ||
               string.Equals(TestClass.NormalizePartOfSpeech(word.PartOfSpeech), SelectedType, StringComparison.Ordinal);
    }

    private static string NormalizeSelectedType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, AllTypes, StringComparison.OrdinalIgnoreCase))
        {
            return AllTypes;
        }

        return TestClass.NormalizePartOfSpeech(value);
    }
}
