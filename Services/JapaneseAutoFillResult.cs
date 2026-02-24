using JPracticeWeb.Models;

namespace JPracticeWeb.Services;

public sealed class JapaneseAutoFillResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string TranslatedDictionaryTerm { get; init; } = string.Empty;
    public TestClass? FilledWord { get; init; }
}
