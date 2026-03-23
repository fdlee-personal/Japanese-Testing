using System.Text.Json;
using JPracticeWeb.Models;

namespace JPracticeWeb.Services;

public static class TestWordStore
{
    private static readonly object _sync = new();
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private static List<TestClass> _items = CreateDefaultItems();
    private static string? _filePath;
    private static bool _initialized;

    public static void Initialize(string filePath, IEnumerable<string>? additionalCandidatePaths = null)
    {
        lock (_sync)
        {
            if (_initialized)
            {
                return;
            }

            _filePath = filePath;
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var mergedItems = new List<TestClass>();
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);
            var candidates = new List<string> { filePath };
            if (additionalCandidatePaths is not null)
            {
                candidates.AddRange(additionalCandidatePaths);
            }

            foreach (var candidate in candidates
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => Path.GetFullPath(p))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                foreach (var item in ReadItemsFromFile(candidate))
                {
                    var key = BuildWordKey(item);
                    if (seenKeys.Add(key))
                    {
                        mergedItems.Add(item);
                    }
                }
            }

            if (mergedItems.Count > 0)
            {
                // Reassign IDs sequentially to keep internal IDs stable and unique after merge.
                _items = mergedItems
                    .OrderBy(x => x.Id)
                    .ThenBy(x => x.KoreanWord, StringComparer.Ordinal)
                    .Select((x, index) => CloneWithId(x, index + 1))
                    .ToList();
            }
            else
            {
                _items = CreateDefaultItems();
            }

            SaveLocked();

            _initialized = true;
        }
    }

    public static IReadOnlyList<TestClass> GetAll()
    {
        lock (_sync)
        {
            return _items.OrderBy(x => x.Id).ToList();
        }
    }

    public static TestClass? GetById(int id)
    {
        lock (_sync)
        {
            var item = _items.FirstOrDefault(x => x.Id == id);
            return item is null ? null : CloneWithId(item, item.Id);
        }
    }

    public static void Add(TestClass item)
    {
        lock (_sync)
        {
            if (item.Id <= 0 || _items.Any(x => x.Id == item.Id))
            {
                item.Id = _items.Count == 0 ? 1 : _items.Max(x => x.Id) + 1;
            }

            _items.Add(item);
            SaveLocked();
        }
    }

    public static bool RemoveById(int id)
    {
        lock (_sync)
        {
            var item = _items.FirstOrDefault(x => x.Id == id);
            if (item is null)
            {
                return false;
            }

            _items.Remove(item);
            SaveLocked();
            return true;
        }
    }

    public static bool Update(TestClass updatedItem)
    {
        lock (_sync)
        {
            var index = _items.FindIndex(x => x.Id == updatedItem.Id);
            if (index < 0)
            {
                return false;
            }

            _items[index] = CloneWithId(updatedItem, updatedItem.Id);
            SaveLocked();
            return true;
        }
    }

    private static void SaveLocked()
    {
        if (string.IsNullOrWhiteSpace(_filePath))
        {
            return;
        }

        var json = JsonSerializer.Serialize(_items.OrderBy(x => x.Id).ToList(), _jsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private static List<TestClass> CreateDefaultItems()
    {
        return
        [
            new()
            {
                Id = 1,
                KoreanWord = "먹다",
                PartOfSpeech = TestClass.PartOfSpeechVerb,
                DictionaryTerm = "食べる",
                DictionaryTermKana = "たべる",
                PoliteForm = "食べます",
                PoliteFormKana = "たべます",
                NegativeForm = "食べない",
                NegativeFormKana = "たべない",
                NegativePoliteForm = "食べません",
                NegativePoliteFormKana = "たべません",
                PastForm = "食べた",
                PastFormKana = "たべた",
                PastPoliteForm = "食べました",
                PastPoliteFormKana = "たべました",
                PastNegativeForm = "食べなかった",
                PastNegativeFormKana = "たべなかった",
                PastNegativePoliteForm = "食べませんでした",
                PastNegativePoliteFormKana = "たべませんでした",
                ConnectiveForm = "食べて",
                ConnectiveFormKana = "たべて"
            },
            new()
            {
                Id = 2,
                KoreanWord = "가다",
                PartOfSpeech = TestClass.PartOfSpeechVerb,
                DictionaryTerm = "行く",
                DictionaryTermKana = "いく",
                PoliteForm = "行きます",
                PoliteFormKana = "いきます",
                NegativeForm = "行かない",
                NegativeFormKana = "いかない",
                NegativePoliteForm = "行きません",
                NegativePoliteFormKana = "いきません",
                PastForm = "行った",
                PastFormKana = "いった",
                PastPoliteForm = "行きました",
                PastPoliteFormKana = "いきました",
                PastNegativeForm = "行かなかった",
                PastNegativeFormKana = "いかなかった",
                PastNegativePoliteForm = "行きませんでした",
                PastNegativePoliteFormKana = "いきませんでした",
                ConnectiveForm = "行って",
                ConnectiveFormKana = "いって"
            }
        ];
    }

    private static IEnumerable<TestClass> ReadItemsFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var loaded = JsonSerializer.Deserialize<List<TestClass>>(json, _jsonOptions);
            return loaded ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string BuildWordKey(TestClass item)
    {
        return string.Join("||",
            item.KoreanWord,
            item.PartOfSpeech,
            item.DictionaryTerm,
            item.PoliteForm,
            item.NegativeForm,
            item.NegativePoliteForm,
            item.PastForm,
            item.PastFormKana,
            item.PastPoliteForm,
            item.PastPoliteFormKana,
            item.PastNegativeForm,
            item.PastNegativeFormKana,
            item.PastNegativePoliteForm,
            item.PastNegativePoliteFormKana,
            item.ConnectiveForm,
            item.ConnectiveFormKana,
            item.DictionaryTermKana,
            item.PoliteFormKana,
            item.NegativeFormKana,
            item.NegativePoliteFormKana);
    }

    private static TestClass CloneWithId(TestClass source, int id)
    {
        return new TestClass
        {
            Id = id,
            KoreanWord = source.KoreanWord,
            PartOfSpeech = TestClass.NormalizePartOfSpeech(source.PartOfSpeech),
            DictionaryTerm = source.DictionaryTerm,
            DictionaryTermKana = source.DictionaryTermKana,
            PoliteForm = source.PoliteForm,
            PoliteFormKana = source.PoliteFormKana,
            NegativeForm = source.NegativeForm,
            NegativeFormKana = source.NegativeFormKana,
            NegativePoliteForm = source.NegativePoliteForm,
            NegativePoliteFormKana = source.NegativePoliteFormKana,
            PastForm = source.PastForm,
            PastFormKana = source.PastFormKana,
            PastPoliteForm = source.PastPoliteForm,
            PastPoliteFormKana = source.PastPoliteFormKana,
            PastNegativeForm = source.PastNegativeForm,
            PastNegativeFormKana = source.PastNegativeFormKana,
            PastNegativePoliteForm = source.PastNegativePoliteForm,
            PastNegativePoliteFormKana = source.PastNegativePoliteFormKana,
            ConnectiveForm = source.ConnectiveForm,
            ConnectiveFormKana = source.ConnectiveFormKana
        };
    }
}
