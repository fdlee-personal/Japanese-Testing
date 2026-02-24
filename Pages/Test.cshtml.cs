using JPracticeWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace JPracticeWeb.Pages;

public class TestModel : PageModel
{
    private const string ChoiceMode = "choice";
    private const string InputMode = "input";

    [BindProperty]
    public TestState State { get; set; } = new();

    [BindProperty]
    public string SelectedChoice { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "답을 입력하세요.")]
    public string UserInputAnswer { get; set; } = string.Empty;

    public bool HasQuestion { get; private set; }
    public bool IsChoiceMode => string.Equals(State.Mode, ChoiceMode, StringComparison.Ordinal);
    public bool IsInputMode => string.Equals(State.Mode, InputMode, StringComparison.Ordinal);
    public bool IsAnswered { get; private set; }
    public bool IsCorrect { get; private set; }
    public string ResultMessage { get; private set; } = string.Empty;
    public string CorrectAnswerDisplay => BuildAnswerDisplay(State.CorrectTerm, State.CorrectKana);
    public IReadOnlyList<string> ChoiceOptions { get; private set; } = [];

    public string GetChoiceDisplayLabel(string option)
    {
        if (!IsAnswered)
        {
            return option;
        }

        var counterpart = GetChoiceCounterpart(option);
        if (string.IsNullOrWhiteSpace(counterpart) || string.Equals(counterpart, option, StringComparison.Ordinal))
        {
            return option;
        }

        return $"{option} ({counterpart})";
    }

    public void OnGet()
    {
        GenerateQuestion();
    }

    public IActionResult OnPostChoice()
    {
        RestoreQuestionFromState();
        if (!HasQuestion)
        {
            return Page();
        }

        IsAnswered = true;
        var selected = SelectedChoice?.Trim() ?? string.Empty;
        var correctChoiceValue = string.IsNullOrWhiteSpace(State.CorrectChoiceValue)
            ? State.CorrectTerm
            : State.CorrectChoiceValue;
        IsCorrect =
            string.Equals(selected, correctChoiceValue, StringComparison.Ordinal) ||
            string.Equals(selected, State.CorrectTerm, StringComparison.Ordinal) ||
            (!string.IsNullOrWhiteSpace(State.CorrectKana) &&
             string.Equals(selected, State.CorrectKana, StringComparison.Ordinal));
        ResultMessage = IsCorrect
            ? "정답입니다."
            : $"정답: {CorrectAnswerDisplay}";

        return Page();
    }

    public IActionResult OnPostInput()
    {
        RestoreQuestionFromState();
        if (!HasQuestion)
        {
            return Page();
        }

        var answer = UserInputAnswer?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(answer))
        {
            ModelState.AddModelError(nameof(UserInputAnswer), "답을 입력하세요.");
            return Page();
        }

        IsAnswered = true;
        IsCorrect =
            string.Equals(answer, State.CorrectTerm, StringComparison.Ordinal) ||
            (!string.IsNullOrWhiteSpace(State.CorrectKana) &&
             string.Equals(answer, State.CorrectKana, StringComparison.Ordinal));

        ResultMessage = IsCorrect
            ? "정답입니다."
            : $"틀렸습니다. 정답은 {CorrectAnswerDisplay} 입니다.";

        if (!IsCorrect)
        {
            UserInputAnswer = string.IsNullOrWhiteSpace(answer)
                ? CorrectAnswerDisplay
                : $"{answer}{Environment.NewLine}{Environment.NewLine}{CorrectAnswerDisplay}";
            ModelState.Remove(nameof(UserInputAnswer));
        }

        return Page();
    }

    private void GenerateQuestion()
    {
        var items = TestWordStore.GetAll();
        if (items.Count == 0)
        {
            HasQuestion = false;
            return;
        }

        var selectedItem = items[Random.Shared.Next(items.Count)];
        var terms = selectedItem.GetJapaneseTerms();
        var selectedTerm = terms[Random.Shared.Next(terms.Count)];
        var mode = Random.Shared.Next(2) == 0 ? ChoiceMode : InputMode;

        State = new TestState
        {
            KoreanWord = selectedItem.KoreanWord,
            FormName = selectedTerm.Label,
            CorrectTerm = selectedTerm.Term,
            CorrectKana = selectedTerm.Kana,
            Mode = mode
        };

        if (mode == ChoiceMode)
        {
            var choiceBuild = BuildChoiceOptions(items, selectedTerm.Label, selectedTerm.Term, selectedTerm.Kana);
            State.CorrectChoiceValue = choiceBuild.CorrectChoiceValue;
            State.Choice1 = choiceBuild.Options.ElementAtOrDefault(0) ?? choiceBuild.CorrectChoiceValue;
            State.Choice2 = choiceBuild.Options.ElementAtOrDefault(1) ?? choiceBuild.CorrectChoiceValue;
            State.Choice3 = choiceBuild.Options.ElementAtOrDefault(2) ?? choiceBuild.CorrectChoiceValue;
            State.Choice1Counterpart = choiceBuild.Counterparts.ElementAtOrDefault(0) ?? string.Empty;
            State.Choice2Counterpart = choiceBuild.Counterparts.ElementAtOrDefault(1) ?? string.Empty;
            State.Choice3Counterpart = choiceBuild.Counterparts.ElementAtOrDefault(2) ?? string.Empty;
        }

        RestoreQuestionFromState();
    }

    private void RestoreQuestionFromState()
    {
        if (string.IsNullOrWhiteSpace(State.KoreanWord) ||
            string.IsNullOrWhiteSpace(State.FormName) ||
            string.IsNullOrWhiteSpace(State.CorrectTerm))
        {
            HasQuestion = false;
            ChoiceOptions = [];
            return;
        }

        HasQuestion = true;

        if (IsChoiceMode)
        {
            ChoiceOptions = new[] { State.Choice1, State.Choice2, State.Choice3 }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            // Ensure the correct answer always exists in options if posted state was malformed.
            var expectedChoiceValue = string.IsNullOrWhiteSpace(State.CorrectChoiceValue)
                ? State.CorrectTerm
                : State.CorrectChoiceValue;
            if (!ChoiceOptions.Contains(expectedChoiceValue, StringComparer.Ordinal))
            {
                ChoiceOptions = [.. ChoiceOptions.Take(2), expectedChoiceValue];
            }
        }
        else
        {
            ChoiceOptions = [];
        }
    }

    private static (List<string> Options, List<string> Counterparts, string CorrectChoiceValue) BuildChoiceOptions(
        IReadOnlyList<Models.TestClass> items,
        string formLabel,
        string correctTerm,
        string correctKana)
    {
        var sameFormTerms = items
            .SelectMany(x => x.GetJapaneseTerms())
            .Where(x => string.Equals(x.Label, formLabel, StringComparison.Ordinal))
            .Where(x => !string.IsNullOrWhiteSpace(x.Term))
            .Select(x => new ChoiceTerm(x.Term, x.Kana))
            .GroupBy(x => x.Term, StringComparer.Ordinal)
            .Select(g => g.First())
            .Where(x => !string.Equals(x.Term, correctTerm, StringComparison.Ordinal))
            .ToList();

        var allTerms = items
            .SelectMany(x => x.GetJapaneseTerms())
            .Where(x => !string.IsNullOrWhiteSpace(x.Term))
            .Select(x => new ChoiceTerm(x.Term, x.Kana))
            .GroupBy(x => x.Term, StringComparer.Ordinal)
            .Select(g => g.First())
            .Where(x => !string.Equals(x.Term, correctTerm, StringComparison.Ordinal))
            .ToList();

        var distractors = PickRandomDistinct(sameFormTerms, 2);
        if (distractors.Count < 2)
        {
            foreach (var term in PickRandomDistinct(allTerms, 2 - distractors.Count))
            {
                if (!distractors.Contains(term))
                {
                    distractors.Add(term);
                }
            }
        }

        var correctChoiceValue = ChooseRandomDisplay(correctTerm, correctKana);
        var usedDisplays = new HashSet<string>(StringComparer.Ordinal) { correctChoiceValue };
        var distractorDisplays = new List<string>(2);
        foreach (var distractor in distractors)
        {
            var display = ChooseRandomDisplayAvoidingDuplicate(distractor.Term, distractor.Kana, usedDisplays);
            if (usedDisplays.Add(display))
            {
                distractorDisplays.Add(display);
            }
        }

        while (distractors.Count < 2)
        {
            distractors.Add(new ChoiceTerm(correctTerm, correctKana));
        }

        while (distractorDisplays.Count < 2)
        {
            distractorDisplays.Add(correctChoiceValue);
        }

        var correctCounterpart = GetCounterpartForDisplay(correctChoiceValue, correctTerm, correctKana);
        var distractorCounterpart1 = GetCounterpartForDisplay(distractorDisplays[0], distractors[0].Term, distractors[0].Kana);
        var distractorCounterpart2 = GetCounterpartForDisplay(distractorDisplays[1], distractors[1].Term, distractors[1].Kana);

        var finalPairs = new List<ChoiceDisplay>
        {
            new(correctChoiceValue, correctCounterpart),
            new(distractorDisplays[0], distractorCounterpart1),
            new(distractorDisplays[1], distractorCounterpart2)
        };

        Shuffle(finalPairs);
        return (finalPairs.Select(x => x.Value).ToList(), finalPairs.Select(x => x.Counterpart).ToList(), correctChoiceValue);
    }

    private static List<ChoiceTerm> PickRandomDistinct(List<ChoiceTerm> source, int count)
    {
        var copy = source.ToList();
        Shuffle(copy);
        return copy.Take(count).ToList();
    }

    private static string ChooseRandomDisplay(string term, string kana)
    {
        if (string.IsNullOrWhiteSpace(kana))
        {
            return term;
        }

        return Random.Shared.Next(2) == 0 ? term : kana;
    }

    private static string ChooseRandomDisplayAvoidingDuplicate(string term, string kana, ISet<string> usedDisplays)
    {
        var first = ChooseRandomDisplay(term, kana);
        if (!usedDisplays.Contains(first))
        {
            return first;
        }

        if (!string.IsNullOrWhiteSpace(kana))
        {
            var alternate = string.Equals(first, term, StringComparison.Ordinal) ? kana : term;
            if (!usedDisplays.Contains(alternate))
            {
                return alternate;
            }
        }

        return first;
    }

    private string GetChoiceCounterpart(string option)
    {
        if (string.Equals(State.Choice1, option, StringComparison.Ordinal))
        {
            return State.Choice1Counterpart;
        }

        if (string.Equals(State.Choice2, option, StringComparison.Ordinal))
        {
            return State.Choice2Counterpart;
        }

        if (string.Equals(State.Choice3, option, StringComparison.Ordinal))
        {
            return State.Choice3Counterpart;
        }

        if (string.Equals(option, State.CorrectTerm, StringComparison.Ordinal))
        {
            return string.IsNullOrWhiteSpace(State.CorrectKana) || string.Equals(State.CorrectKana, option, StringComparison.Ordinal)
                ? string.Empty
                : State.CorrectKana;
        }

        if (!string.IsNullOrWhiteSpace(State.CorrectKana) &&
            string.Equals(option, State.CorrectKana, StringComparison.Ordinal))
        {
            return string.Equals(State.CorrectTerm, option, StringComparison.Ordinal) ? string.Empty : State.CorrectTerm;
        }

        return string.Empty;
    }

    private static string GetCounterpartForDisplay(string displayValue, string term, string kana)
    {
        if (string.IsNullOrWhiteSpace(kana))
        {
            return string.Empty;
        }

        if (string.Equals(displayValue, term, StringComparison.Ordinal))
        {
            return string.Equals(kana, displayValue, StringComparison.Ordinal) ? string.Empty : kana;
        }

        if (string.Equals(displayValue, kana, StringComparison.Ordinal))
        {
            return string.Equals(term, displayValue, StringComparison.Ordinal) ? string.Empty : term;
        }

        return string.Empty;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static string BuildAnswerDisplay(string term, string kana)
    {
        if (string.IsNullOrWhiteSpace(kana))
        {
            return term;
        }

        return $"{term} ({kana})";
    }

    public sealed class TestState
    {
        public string KoreanWord { get; set; } = string.Empty;
        public string FormName { get; set; } = string.Empty;
        public string CorrectTerm { get; set; } = string.Empty;
        public string CorrectKana { get; set; } = string.Empty;
        public string CorrectChoiceValue { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public string Choice1 { get; set; } = string.Empty;
        public string Choice1Counterpart { get; set; } = string.Empty;
        public string Choice2 { get; set; } = string.Empty;
        public string Choice2Counterpart { get; set; } = string.Empty;
        public string Choice3 { get; set; } = string.Empty;
        public string Choice3Counterpart { get; set; } = string.Empty;
    }

    private readonly record struct ChoiceTerm(string Term, string Kana);
    private readonly record struct ChoiceDisplay(string Value, string Counterpart);
}
