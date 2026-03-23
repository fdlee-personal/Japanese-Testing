using System.Net.Http.Json;
using System.Text.Json.Serialization;
using JPracticeWeb.Models;

namespace JPracticeWeb.Services;

public interface IJapaneseAutoFillService
{
    Task<JapaneseAutoFillResult> AutoFillFromKoreanAsync(string koreanWord, string? partOfSpeech = null, CancellationToken cancellationToken = default);
    Task<JapaneseAutoFillResult> AutoFillFromJapaneseDictionaryAsync(string japaneseDictionaryTerm, string? koreanWord = null, string? partOfSpeech = null, CancellationToken cancellationToken = default);
}

public sealed class JapaneseAutoFillService(HttpClient httpClient) : IJapaneseAutoFillService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<JapaneseAutoFillResult> AutoFillFromKoreanAsync(string koreanWord, string? partOfSpeech = null, CancellationToken cancellationToken = default)
    {
        var trimmedKorean = koreanWord.Trim();
        if (string.IsNullOrWhiteSpace(trimmedKorean))
        {
            return new JapaneseAutoFillResult
            {
                Success = false,
                Message = "Enter a Korean word first."
            };
        }

        try
        {
            var translated = await TranslateKoreanToJapaneseAsync(trimmedKorean, cancellationToken);
            return await BuildFromJapaneseDictionaryAsync(
                translated,
                trimmedKorean,
                partOfSpeech,
                "No Japanese translation was returned.",
                "web",
                cancellationToken);
        }
        catch (Exception ex)
        {
            return new JapaneseAutoFillResult
            {
                Success = false,
                Message = $"Auto-fill failed: {ex.Message}"
            };
        }
    }

    public async Task<JapaneseAutoFillResult> AutoFillFromJapaneseDictionaryAsync(string japaneseDictionaryTerm, string? koreanWord = null, string? partOfSpeech = null, CancellationToken cancellationToken = default)
    {
        var term = japaneseDictionaryTerm?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(term))
        {
            return new JapaneseAutoFillResult
            {
                Success = false,
                Message = "사전형(일본어)을 먼저 입력하세요."
            };
        }

        try
        {
            return await BuildFromJapaneseDictionaryAsync(
                term,
                koreanWord?.Trim() ?? string.Empty,
                partOfSpeech,
                "일본어 사전형이 비어 있습니다.",
                "사전형",
                cancellationToken);
        }
        catch (Exception ex)
        {
            return new JapaneseAutoFillResult
            {
                Success = false,
                Message = $"사전형 자동채우기 실패: {ex.Message}"
            };
        }
    }

    private async Task<string> TranslateKoreanToJapaneseAsync(string koreanWord, CancellationToken cancellationToken)
    {
        var url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(koreanWord)}&langpair=ko|ja";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("JPracticeWeb/1.0");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Translation request failed ({(int)response.StatusCode}).");
        }

        var payload = await response.Content.ReadFromJsonAsync<MyMemoryResponse>(cancellationToken: cancellationToken);
        return NormalizeCandidate(payload?.ResponseData?.TranslatedText);
    }

    private async Task<string> TranslateJapaneseToKoreanAsync(string japaneseWord, CancellationToken cancellationToken)
    {
        var url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(japaneseWord)}&langpair=ja|ko";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("JPracticeWeb/1.0");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Translation request failed ({(int)response.StatusCode}).");
        }

        var payload = await response.Content.ReadFromJsonAsync<MyMemoryResponse>(cancellationToken: cancellationToken);
        return NormalizeCandidate(payload?.ResponseData?.TranslatedText);
    }

    private async Task<string?> TryGetJapaneseReadingAsync(string dictionaryTerm, CancellationToken cancellationToken)
    {
        var url = $"https://jisho.org/api/v1/search/words?keyword={Uri.EscapeDataString(dictionaryTerm)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("JPracticeWeb/1.0");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<JishoResponse>(cancellationToken: cancellationToken);
        if (payload?.Data is null || payload.Data.Count == 0)
        {
            return null;
        }

        foreach (var entry in payload.Data)
        {
            foreach (var jp in entry.Japanese)
            {
                if (string.Equals(jp.Word, dictionaryTerm, StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(jp.Reading))
                {
                    return jp.Reading.Trim();
                }
            }
        }

        return payload.Data
            .SelectMany(x => x.Japanese)
            .Select(x => x.Reading?.Trim())
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
    }

    private static void CopyKanaForms(TestClass target, TestClass kanaSource)
    {
        target.DictionaryTermKana = kanaSource.DictionaryTerm;
        target.PoliteFormKana = kanaSource.PoliteForm;
        target.NegativeFormKana = kanaSource.NegativeForm;
        target.NegativePoliteFormKana = kanaSource.NegativePoliteForm;
        target.PastFormKana = kanaSource.PastForm;
        target.PastPoliteFormKana = kanaSource.PastPoliteForm;
        target.PastNegativeFormKana = kanaSource.PastNegativeForm;
        target.PastNegativePoliteFormKana = kanaSource.PastNegativePoliteForm;
        target.ConnectiveFormKana = kanaSource.ConnectiveForm;
    }

    private static string NormalizeCandidate(string? translatedText)
    {
        if (string.IsNullOrWhiteSpace(translatedText))
        {
            return string.Empty;
        }

        var text = translatedText.Trim().Trim('。', '.', '!', '！', '?', '？', '、');
        var separators = new[] { ',', '，', ';', '；', '/', '|' };
        var first = text.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        return first ?? text;
    }

    private async Task<JapaneseAutoFillResult> BuildFromJapaneseDictionaryAsync(
        string dictionaryTerm,
        string koreanWord,
        string? partOfSpeech,
        string emptyMessage,
        string sourceLabel,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dictionaryTerm))
        {
            return new JapaneseAutoFillResult
            {
                Success = false,
                Message = emptyMessage
            };
        }

        var normalizedPartOfSpeech = TestClass.NormalizePartOfSpeech(partOfSpeech);
        if (!string.Equals(normalizedPartOfSpeech, TestClass.PartOfSpeechVerb, StringComparison.Ordinal))
        {
            return await BuildNonVerbWordAsync(
                dictionaryTerm,
                koreanWord,
                normalizedPartOfSpeech,
                sourceLabel,
                cancellationToken);
        }

        if (!JapaneseVerbConjugator.TryConjugate(dictionaryTerm, out var kanjiWord, out var error))
        {
            return new JapaneseAutoFillResult
            {
                Success = false,
                Message = $"{error} ({sourceLabel} 결과: {dictionaryTerm})",
                TranslatedDictionaryTerm = dictionaryTerm
            };
        }

        var resolvedKoreanWord = koreanWord?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(resolvedKoreanWord))
        {
            try
            {
                resolvedKoreanWord = await TranslateJapaneseToKoreanAsync(dictionaryTerm, cancellationToken);
            }
            catch
            {
            }
        }

        if (!string.IsNullOrWhiteSpace(resolvedKoreanWord))
        {
            kanjiWord.KoreanWord = resolvedKoreanWord;
        }

        kanjiWord.PartOfSpeech = normalizedPartOfSpeech;

        var kanaDictionary = await TryGetJapaneseReadingAsync(dictionaryTerm, cancellationToken);
        var kanaWasAutoFilled = false;

        if (!string.IsNullOrWhiteSpace(kanaDictionary) &&
            JapaneseVerbConjugator.TryConjugate(kanaDictionary, out var kanaWord, out _))
        {
            CopyKanaForms(kanjiWord, kanaWord);
            kanaWasAutoFilled = true;
        }
        else
        {
            CopyKanaForms(kanjiWord, kanjiWord);
        }

        return new JapaneseAutoFillResult
        {
            Success = true,
            Message = kanaWasAutoFilled
                ? $"{sourceLabel} 기준으로 활용형/가나를 자동채웠습니다: {dictionaryTerm}"
                : $"{sourceLabel} 기준으로 활용형을 자동채웠습니다: {dictionaryTerm} (가나 조회 실패, 한자값 복사)",
            TranslatedDictionaryTerm = dictionaryTerm,
            FilledWord = kanjiWord
        };
    }

    private async Task<JapaneseAutoFillResult> BuildNonVerbWordAsync(
        string dictionaryTerm,
        string koreanWord,
        string partOfSpeech,
        string sourceLabel,
        CancellationToken cancellationToken)
    {
        var word = new TestClass
        {
            PartOfSpeech = partOfSpeech,
            DictionaryTerm = dictionaryTerm.Trim()
        };

        var resolvedKoreanWord = koreanWord?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(resolvedKoreanWord))
        {
            try
            {
                resolvedKoreanWord = await TranslateJapaneseToKoreanAsync(dictionaryTerm, cancellationToken);
            }
            catch
            {
            }
        }

        word.KoreanWord = resolvedKoreanWord;

        var kanaDictionary = await TryGetJapaneseReadingAsync(dictionaryTerm, cancellationToken);
        word.DictionaryTermKana = string.IsNullOrWhiteSpace(kanaDictionary)
            ? word.DictionaryTerm
            : kanaDictionary.Trim();

        return new JapaneseAutoFillResult
        {
            Success = true,
            Message = string.IsNullOrWhiteSpace(kanaDictionary)
                ? $"{sourceLabel} 기준으로 기본형을 자동채웠습니다: {dictionaryTerm} (가나 조회 실패, 원문 복사)"
                : $"{sourceLabel} 기준으로 기본형과 가나를 자동채웠습니다: {dictionaryTerm}",
            TranslatedDictionaryTerm = dictionaryTerm,
            FilledWord = word
        };
    }

    private sealed class MyMemoryResponse
    {
        [JsonPropertyName("responseData")]
        public MyMemoryResponseData? ResponseData { get; set; }
    }

    private sealed class MyMemoryResponseData
    {
        [JsonPropertyName("translatedText")]
        public string? TranslatedText { get; set; }
    }

    private sealed class JishoResponse
    {
        [JsonPropertyName("data")]
        public List<JishoEntry> Data { get; set; } = [];
    }

    private sealed class JishoEntry
    {
        [JsonPropertyName("japanese")]
        public List<JishoJapanese> Japanese { get; set; } = [];
    }

    private sealed class JishoJapanese
    {
        [JsonPropertyName("word")]
        public string? Word { get; set; }

        [JsonPropertyName("reading")]
        public string? Reading { get; set; }
    }
}

internal static class JapaneseVerbConjugator
{
    public static bool TryConjugate(string dictionaryTerm, out TestClass word, out string error)
    {
        word = new TestClass();
        error = string.Empty;

        var term = dictionaryTerm.Trim();
        if (string.IsNullOrWhiteSpace(term))
        {
            error = "Dictionary term was empty";
            return false;
        }

        if (term.EndsWith("する", StringComparison.Ordinal))
        {
            var stem = term[..^2];
            Fill(
                word,
                term,
                stem + "します",
                stem + "しない",
                stem + "しません",
                stem + "した",
                stem + "しました",
                stem + "しなかった",
                stem + "しませんでした",
                stem + "して");
            return true;
        }

        if (term is "来る" or "くる")
        {
            Fill(
                word,
                term,
                "来ます",
                "来ない",
                "来ません",
                "来た",
                "来ました",
                "来なかった",
                "来ませんでした",
                "来て");
            return true;
        }

        var last = term[^1];
        if (!IsSupportedVerbEnding(last))
        {
            error = "Web translation was not recognized as a Japanese verb dictionary form";
            return false;
        }

        if (IsIchidan(term))
        {
            var stem = term[..^1];
            var negative = stem + "ない";
            var politeStem = stem;

            Fill(
                word,
                term,
                politeStem + "ます",
                negative,
                politeStem + "ません",
                stem + "た",
                politeStem + "ました",
                stem + "なかった",
                politeStem + "ませんでした",
                stem + "て");
            return true;
        }

        return TryGodanConjugate(term, out word, out error);
    }

    private static bool TryGodanConjugate(string term, out TestClass word, out string error)
    {
        word = new TestClass();
        error = string.Empty;

        var stem = term[..^1];
        var ending = term[^1];

        if (!TryMapI(ending, out var iEnding) || !TryMapA(ending, out var aEnding))
        {
            error = $"Unsupported godan ending: {ending}";
            return false;
        }

        var politeStem = stem + iEnding;
        var negative = stem + aEnding + "ない";
        var te = GetTeForm(term);
        var ta = GetTaForm(term);

        Fill(
            word,
            term,
            politeStem + "ます",
            negative,
            politeStem + "ません",
            ta,
            politeStem + "ました",
            negative[..^1] + "かった",
            politeStem + "ませんでした",
            te);

        return true;
    }

    private static string GetTeForm(string term)
    {
        if (term is "行く" or "いく")
        {
            return term[..^1] + "って";
        }

        var stem = term[..^1];
        return term[^1] switch
        {
            'う' or 'つ' or 'る' => stem + "って",
            'む' or 'ぶ' or 'ぬ' => stem + "んで",
            'く' => stem + "いて",
            'ぐ' => stem + "いで",
            'す' => stem + "して",
            _ => term
        };
    }

    private static string GetTaForm(string term)
    {
        if (term is "行く" or "いく")
        {
            return term[..^1] + "った";
        }

        var stem = term[..^1];
        return term[^1] switch
        {
            'う' or 'つ' or 'る' => stem + "った",
            'む' or 'ぶ' or 'ぬ' => stem + "んだ",
            'く' => stem + "いた",
            'ぐ' => stem + "いだ",
            'す' => stem + "した",
            _ => term
        };
    }

    private static bool IsIchidan(string term)
    {
        if (!term.EndsWith("る", StringComparison.Ordinal) || term.Length < 2)
        {
            return false;
        }

        var prev = term[^2];
        return prev is 'い' or 'き' or 'ぎ' or 'し' or 'じ' or 'ち' or 'に' or 'ひ' or 'び' or 'ぴ' or 'み' or 'り'
            or 'え' or 'け' or 'げ' or 'せ' or 'ぜ' or 'て' or 'で' or 'ね' or 'へ' or 'べ' or 'ぺ' or 'め' or 'れ'
            or 'イ' or 'キ' or 'ギ' or 'シ' or 'ジ' or 'チ' or 'ニ' or 'ヒ' or 'ビ' or 'ピ' or 'ミ' or 'リ'
            or 'エ' or 'ケ' or 'ゲ' or 'セ' or 'ゼ' or 'テ' or 'デ' or 'ネ' or 'ヘ' or 'ベ' or 'ペ' or 'メ' or 'レ';
    }

    private static bool IsSupportedVerbEnding(char c)
    {
        return c is 'る' or 'う' or 'つ' or 'む' or 'ぶ' or 'ぬ' or 'く' or 'ぐ' or 'す';
    }

    private static bool TryMapI(char ending, out char mapped)
    {
        mapped = ending switch
        {
            'う' => 'い',
            'つ' => 'ち',
            'る' => 'り',
            'む' => 'み',
            'ぶ' => 'び',
            'ぬ' => 'に',
            'く' => 'き',
            'ぐ' => 'ぎ',
            'す' => 'し',
            _ => '\0'
        };

        return mapped != '\0';
    }

    private static bool TryMapA(char ending, out char mapped)
    {
        mapped = ending switch
        {
            'う' => 'わ',
            'つ' => 'た',
            'る' => 'ら',
            'む' => 'ま',
            'ぶ' => 'ば',
            'ぬ' => 'な',
            'く' => 'か',
            'ぐ' => 'が',
            'す' => 'さ',
            _ => '\0'
        };

        return mapped != '\0';
    }

    private static void Fill(
        TestClass target,
        string dictionary,
        string polite,
        string negative,
        string negativePolite,
        string past,
        string pastPolite,
        string pastNegative,
        string pastNegativePolite,
        string connective)
    {
        target.DictionaryTerm = dictionary;
        target.PoliteForm = polite;
        target.NegativeForm = negative;
        target.NegativePoliteForm = negativePolite;
        target.PastForm = past;
        target.PastPoliteForm = pastPolite;
        target.PastNegativeForm = pastNegative;
        target.PastNegativePoliteForm = pastNegativePolite;
        target.ConnectiveForm = connective;
    }
}
