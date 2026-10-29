using ScaleSim.Core.Abstractions;
using ScaleSim.Core.Storage;

namespace ScaleSim.Core;

/// <summary>Registry of the simulated scales: create, remove, persist, restore.</summary>
public sealed class ScaleManager
{
    private readonly ScaleCatalog _catalog;
    private readonly ScaleStore _store;
    private readonly object _gate = new();
    private readonly List<ScaleInstance> _instances = new();
    private Timer? _saveTimer;

    public ScaleManager(ScaleCatalog catalog, ScaleStore store)
    {
        _catalog = catalog;
        _store = store;
    }

    public ScaleCatalog Catalog => _catalog;

    public IReadOnlyList<ScaleInstance> Instances
    {
        get { lock (_gate) return _instances.ToList(); }
    }

    public event Action<ScaleInstance>? Added;
    public event Action<ScaleInstance>? Removed;
    public event Action<ScaleInstance>? InstanceChanged;
    public event Action<ScaleInstance, int>? InstanceBeeped;

    public ScaleInstance? Get(string id)
    {
        lock (_gate) return _instances.FirstOrDefault(i => i.Id == id);
    }

    /// <summary>Loads the saved scales (starting each one). Models no longer in the catalog are skipped.</summary>
    public void Load()
    {
        foreach (var rec in _store.Load())
        {
            var model = _catalog.Find(rec.ModelId);
            if (model is null) continue;
            Register(new ScaleInstance(rec.Id, model, rec.Options, rec.State));
        }
    }

    public ScaleInstance Add(string modelId, string? name = null)
    {
        var model = _catalog.Find(modelId) ?? throw new ArgumentException($"Unknown model '{modelId}'.");
        var options = model.CreateDefaultOptions();
        options.Port = NextFreePort(options.Port);
        options.Name = !string.IsNullOrWhiteSpace(name) ? name!.Trim() : UniqueName(options.Name);
        var instance = new ScaleInstance(Guid.NewGuid().ToString("N"), model, options);
        Register(instance);
        RequestSave();
        return instance;
    }

    public void Remove(string id)
    {
        ScaleInstance? instance;
        lock (_gate)
        {
            instance = _instances.FirstOrDefault(i => i.Id == id);
            if (instance is null) return;
            _instances.Remove(instance);
        }
        instance.Stop();
        Removed?.Invoke(instance);
        RequestSave();
    }

    private void Register(ScaleInstance instance)
    {
        lock (_gate) _instances.Add(instance);
        instance.Changed += i => { InstanceChanged?.Invoke(i); RequestSave(); };
        instance.Beeped += ms => InstanceBeeped?.Invoke(instance, ms);
        instance.Start();
        Added?.Invoke(instance);
    }

    private int NextFreePort(int preferred)
    {
        var used = Instances.Select(i => i.Options.Port).ToHashSet();
        var port = preferred;
        while (used.Contains(port)) port++;
        return port;
    }

    private string UniqueName(string baseName)
    {
        var names = Instances.Select(i => i.Options.Name).ToHashSet();
        if (!names.Contains(baseName)) return baseName;
        for (var n = 2; ; n++)
            if (!names.Contains($"{baseName} {n}")) return $"{baseName} {n}";
    }

    /// <summary>Debounced save: weight sliders fire many changes per second.</summary>
    public void RequestSave()
    {
        lock (_gate)
        {
            _saveTimer?.Dispose();
            _saveTimer = new Timer(_ => SaveNow(), null, 800, Timeout.Infinite);
        }
    }

    public void SaveNow()
    {
        List<ScaleRecord> records;
        lock (_gate)
            records = _instances.Select(i => new ScaleRecord(i.Id, i.Model.Info.Id, i.Options, i.State.Export())).ToList();
        _store.Save(records);
    }

    public void StopAll()
    {
        foreach (var i in Instances) i.Server.Stop();
    }
}
