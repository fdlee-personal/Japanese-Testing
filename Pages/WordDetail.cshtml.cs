using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JPracticeWeb.Models;
using JPracticeWeb.Services;

namespace JPracticeWeb.Pages;

public class WordDetailModel : PageModel
{
    public TestClass? Word { get; private set; }
    public int ReturnPage { get; private set; } = 1;

    [TempData]
    public string? StatusMessage { get; set; }

    public IActionResult OnGet(int id, int? p)
    {
        ReturnPage = Math.Max(1, p.GetValueOrDefault(1));
        Word = TestWordStore.GetAll().FirstOrDefault(x => x.Id == id);
        if (Word is null)
        {
            return NotFound();
        }

        return Page();
    }

    public IActionResult OnPostDelete(int id, int? p)
    {
        var returnPage = Math.Max(1, p.GetValueOrDefault(1));
        var removed = TestWordStore.RemoveById(id);
        TempData["WordListMessage"] = removed ? "단어를 삭제했습니다." : "삭제할 단어를 찾을 수 없습니다.";
        return RedirectToPage("/WordList", new { p = returnPage });
    }
}
