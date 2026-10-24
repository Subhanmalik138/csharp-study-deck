using System.Text.Json;

namespace StudyDeck;

/// <summary>JSON file store with atomic writes: serialize to a temp file in
/// the same directory and rename over the original, so a crash mid-save
/// can never leave a half-written data file. A corrupt data file is
/// quarantined (never deleted) and loading starts from an empty library.</summary>
public sealed class JsonStudyStore : IStudyStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string Path { get; }
    public StudyData Data { get; private set; }

    public JsonStudyStore(string path)
    {
        Path = path;
        Data = Load(path);
    }

    public void Save()
    {
        var dir = System.IO.Path.GetDirectoryName(Path)!;
        Directory.CreateDirectory(dir);
        var tmp = Path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(Data, JsonOptions));
        File.Move(tmp, Path, overwrite: true);
    }

    private static StudyData Load(string path)
    {
        if (!File.Exists(path)) return new StudyData();
        try
        {
            return JsonSerializer.Deserialize<StudyData>(File.ReadAllText(path), JsonOptions)
                   ?? new StudyData();
        }
        catch (JsonException)
        {
            var quarantine = $"{path}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}";
            File.Move(path, quarantine);
            return new StudyData();
        }
    }
}
