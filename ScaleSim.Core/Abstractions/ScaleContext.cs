namespace ScaleSim.Core.Abstractions;

/// <summary>What a protocol may touch: the simulated physical scale, its settings and the activity log.</summary>
public sealed class ScaleContext
{
    private readonly Action<int> _beep;

    public ScaleContext(ScaleState state, ScaleOptions options, ActivityLog log, Action<int> beep)
    {
        State = state;
        Options = options;
        Log = log;
        _beep = beep;
    }

    public ScaleState State { get; }
    public ScaleOptions Options { get; }
    public ActivityLog Log { get; }

    public void Beep(int milliseconds) => _beep(milliseconds);

    public string Extra(string key, string fallback) =>
        Options.Extra.TryGetValue(key, out var v) && v is not null ? v : fallback;

    public bool ExtraBool(string key, bool fallback) =>
        Options.Extra.TryGetValue(key, out var v) && bool.TryParse(v, out var b) ? b : fallback;
}
