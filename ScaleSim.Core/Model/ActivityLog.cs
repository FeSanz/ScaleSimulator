namespace ScaleSim.Core;

public enum ActivityKind { Rx, Tx, Info, Error }

public sealed record ActivityEntry(long Seq, DateTime Time, ActivityKind Kind, string Client, string Text, bool Continuous);

/// <summary>Bounded, thread-safe record of what crossed the wire; the UI's protocol console.</summary>
public sealed class ActivityLog
{
    private const int Capacity = 2000;
    private readonly object _gate = new();
    private readonly LinkedList<ActivityEntry> _entries = new();
    private long _seq;

    public event Action<ActivityEntry>? Added;
    public event Action? Cleared;

    public ActivityEntry Add(ActivityKind kind, string client, string text, bool continuous = false)
    {
        ActivityEntry entry;
        lock (_gate)
        {
            entry = new ActivityEntry(++_seq, DateTime.Now, kind, client, text, continuous);
            _entries.AddLast(entry);
            while (_entries.Count > Capacity) _entries.RemoveFirst();
        }
        Added?.Invoke(entry);
        return entry;
    }

    public IReadOnlyList<ActivityEntry> Snapshot()
    {
        lock (_gate) return _entries.ToList();
    }

    public void Clear()
    {
        lock (_gate) _entries.Clear();
        Cleared?.Invoke();
    }
}
