using System.Text.Json;

namespace ScaleSim.Core.Storage;

public sealed record ScaleRecord(string Id, string ModelId, ScaleOptions Options, ScaleStateData? State);

/// <summary>Persists the registered scales as JSON (default: %LocalAppData%\ScaleSimulator\scales.json).</summary>
public sealed class ScaleStore
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private readonly string _path;

    public ScaleStore(string path) => _path = path;

    public static string DefaultDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ScaleSimulator");

    public static ScaleStore CreateDefault() => new(Path.Combine(DefaultDirectory, "scales.json"));

    public IReadOnlyList<ScaleRecord> Load()
    {
        try
        {
            if (!File.Exists(_path)) return Array.Empty<ScaleRecord>();
            return JsonSerializer.Deserialize<List<ScaleRecord>>(File.ReadAllText(_path), Json) ?? new List<ScaleRecord>();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // A corrupt file must not stop the app from opening; keep it aside for inspection.
            try { File.Move(_path, _path + ".corrupt", true); } catch { /* best effort */ }
            return Array.Empty<ScaleRecord>();
        }
    }

    public void Save(IReadOnlyList<ScaleRecord> records)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(records, Json));
            File.Move(tmp, _path, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Settings persistence is a convenience; never crash the simulator over it.
        }
    }
}
