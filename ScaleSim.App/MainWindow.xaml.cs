using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using ScaleSim.App.Services;
using ScaleSim.Core;
using ScaleSim.Core.Abstractions;
using ScaleSim.Core.Storage;
using ScaleSim.Radwag;

namespace ScaleSim.App;

public partial class MainWindow : Window
{
    private static readonly JsonSerializerOptions JsonOut = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ScaleManager _manager;
    private readonly DispatcherTimer _flush = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private readonly HashSet<string> _dirty = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<ActivityEntry>> _pendingLog = new();
    private readonly object _dirtyGate = new();
    private int _tick;
    private bool _pageReady;

    public MainWindow()
    {
        InitializeComponent();

        // The catalog is the single place that knows which scale models exist.
        var catalog = new ScaleCatalog().Register(new RadwagModel());
        _manager = new ScaleManager(catalog, ScaleStore.CreateDefault());

        Loaded += async (_, _) => await InitializeAsync();
        Closing += (_, _) =>
        {
            _manager.SaveNow();
            _manager.StopAll();
        };
    }

    private async Task InitializeAsync()
    {
        try
        {
            var dataDir = Path.Combine(ScaleStore.DefaultDirectory, "WebView2");
            var env = await CoreWebView2Environment.CreateAsync(null, dataDir);
            await Browser.EnsureCoreWebView2Async(env);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "No se pudo iniciar el componente Microsoft Edge WebView2 Runtime, necesario para mostrar la interfaz.\n\n" +
                "Instálalo desde https://developer.microsoft.com/microsoft-edge/webview2/ y vuelve a abrir la aplicación.\n\n" + ex.Message,
                "Simulador de Básculas", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
            return;
        }

        var web = Browser.CoreWebView2;
        web.Settings.AreDefaultContextMenusEnabled = false;
        web.Settings.AreDevToolsEnabled = Debugger.IsAttached;
        web.Settings.IsStatusBarEnabled = false;
        web.SetVirtualHostNameToFolderMapping("app.scalesim.local",
            Path.Combine(AppContext.BaseDirectory, "wwwroot"), CoreWebView2HostResourceAccessKind.Allow);
        web.WebMessageReceived += OnWebMessage;
        web.NewWindowRequested += (_, e) => e.Handled = true;

        _manager.Added += OnInstanceAdded;
        _manager.Removed += OnInstanceRemoved;
        _manager.InstanceChanged += i => MarkDirty(i.Id);
        _manager.InstanceBeeped += OnBeep;
        _manager.Load();
        if (_manager.Instances.Count == 0)
            _manager.Add(RadwagModel.ModelId);   // first run: a ready-to-use Radwag on :4001

        _flush.Tick += (_, _) => Flush();
        _flush.Start();

        web.Navigate("https://app.scalesim.local/index.html");
    }

    // ---- host -> page ------------------------------------------------------------------------

    private void Post(object message)
    {
        if (!_pageReady) return;
        Browser.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message, JsonOut));
    }

    private void MarkDirty(string id)
    {
        lock (_dirtyGate) _dirty.Add(id);
    }

    private void OnInstanceAdded(ScaleInstance i)
    {
        i.Log.Added += e => _pendingLog.GetOrAdd(i.Id, _ => new ConcurrentQueue<ActivityEntry>()).Enqueue(e);
        i.Log.Cleared += () => Dispatcher.BeginInvoke(() => Post(UiModel.Log(i.Id, Array.Empty<ActivityEntry>(), reset: true)));
        MarkDirty(i.Id);
        Dispatcher.BeginInvoke(() => Post(new { type = "added", id = i.Id }));
    }

    private void OnInstanceRemoved(ScaleInstance i)
    {
        _pendingLog.TryRemove(i.Id, out _);
        Dispatcher.BeginInvoke(() => Post(new { type = "removed", id = i.Id }));
    }

    private void OnBeep(ScaleInstance i, int ms)
    {
        Task.Run(() =>
        {
            try { Console.Beep(1000, ms); } catch { System.Media.SystemSounds.Beep.Play(); }
        });
        Dispatcher.BeginInvoke(() => Post(new { type = "beep", id = i.Id, ms }));
    }

    /// <summary>Coalesces bursts of state/log changes into at most ~12 page updates per second.</summary>
    private void Flush()
    {
        if (!_pageReady) return;
        _tick++;

        // An unstable scale jitters: keep re-reading it so the display shows the noise.
        if (_tick % 3 == 0)
            foreach (var i in _manager.Instances.Where(i => !i.State.Stable)) MarkDirty(i.Id);

        string[] ids;
        lock (_dirtyGate)
        {
            ids = _dirty.ToArray();
            _dirty.Clear();
        }
        foreach (var id in ids)
            if (_manager.Get(id) is { } instance) Post(UiModel.Snapshot(instance));

        foreach (var (id, queue) in _pendingLog)
        {
            var batch = new List<ActivityEntry>();
            while (batch.Count < 200 && queue.TryDequeue(out var e)) batch.Add(e);
            if (batch.Count > 0) Post(UiModel.Log(id, batch));
        }
    }

    private void SendEverything()
    {
        Post(UiModel.Init(_manager.Catalog));
        foreach (var i in _manager.Instances)
        {
            // Entries queued while the page was loading are part of the full history sent below.
            _pendingLog.TryRemove(i.Id, out _);
            Post(new { type = "added", id = i.Id });
            Post(UiModel.Snapshot(i));
            Post(UiModel.Log(i.Id, i.Log.Snapshot(), reset: true));
        }
        Post(new { type = "ready" });
    }

    // ---- page -> host ------------------------------------------------------------------------

    private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            var root = doc.RootElement;
            var action = root.GetProperty("action").GetString();
            var id = root.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            var instance = id is null ? null : _manager.Get(id);

            switch (action)
            {
                case "ready":
                    _pageReady = true;
                    SendEverything();
                    break;
                case "refreshNetwork":
                    Post(UiModel.Init(_manager.Catalog));
                    break;
                case "addScale":
                    {
                        var created = _manager.Add(root.GetProperty("modelId").GetString() ?? "", Str(root, "name"));
                        Post(new { type = "select", id = created.Id });
                    }
                    break;
                case "removeScale" when id is not null:
                    _manager.Remove(id);
                    break;
                case "toggleServer" when instance is not null:
                    if (instance.Status == ServerStatus.Listening) instance.Stop(); else instance.Start();
                    break;
                case "clearLog" when instance is not null:
                    instance.Log.Clear();
                    break;
                case "saveOptions" when instance is not null:
                    {
                        var draft = instance.Options.Clone();
                        ApplyJson(draft, root.GetProperty("options"));
                        var error = instance.ApplyOptions(draft);
                        if (error is not null) Post(new { type = "toast", level = "error", text = error });
                        else Post(new { type = "toast", level = "ok", text = "Ajustes guardados" });
                        MarkDirty(instance.Id);
                    }
                    break;
                default:
                    if (instance is not null) HandleScaleAction(instance, action ?? "", root);
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Bad web message: {ex}");
            Post(new { type = "toast", level = "error", text = "Error interno: " + ex.Message });
        }
    }

    private void HandleScaleAction(ScaleInstance i, string action, JsonElement m)
    {
        var s = i.State;
        var platform = m.TryGetProperty("platform", out var p) && p.TryGetInt32(out var pi) ? pi : s.ActivePlatform;
        switch (action)
        {
            case "setLoad": s.SetLoad(platform, m.GetProperty("value").GetDouble()); break;
            case "addLoad": s.AddLoad(platform, m.GetProperty("delta").GetDouble()); break;
            case "setStable": s.Stable = m.GetProperty("value").GetBoolean(); break;
            case "setUnit": s.Unit = m.GetProperty("value").GetString() ?? s.Unit; break;
            case "setMode": s.Mode = m.GetProperty("value").GetInt32(); break;
            case "setPlatform": s.ActivePlatform = m.GetProperty("value").GetInt32(); break;
            case "clearTare": s.SetTare(platform, 0); break;
            case "zero":
                if (s.Zero(platform) == ZeroResult.OutOfRange)
                    Post(new { type = "toast", level = "error", text = "Fuera del rango de puesta a cero (±2 % de la capacidad)" });
                break;
            case "tare":
                if (s.Tare(platform) == TareResult.BelowRange)
                    Post(new { type = "toast", level = "error", text = "No hay carga positiva que tarar" });
                break;
        }
        MarkDirty(i.Id);
    }

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    /// <summary>Copies the fields present in <paramref name="json"/> onto <paramref name="o"/>.</summary>
    private static void ApplyJson(ScaleOptions o, JsonElement json)
    {
        string? S(string n) => Str(json, n);
        bool Has(string n, out JsonElement v) => json.TryGetProperty(n, out v) && v.ValueKind != JsonValueKind.Null;

        if (S("name") is { } name) o.Name = name.Trim();
        if (S("baseUnit") is { } unit) o.BaseUnit = unit;
        if (S("serialNumber") is { } serial) o.SerialNumber = serial.Trim();
        if (Has("port", out var v)) o.Port = v.GetInt32();
        if (Has("maxCapacity", out v)) o.MaxCapacity = v.GetDouble();
        if (Has("decimals", out v)) o.Decimals = v.GetInt32();
        if (Has("platforms", out v)) o.Platforms = v.GetInt32();
        if (Has("stabilizationMs", out v)) o.StabilizationMs = v.GetInt32();
        if (Has("tareMs", out v)) o.TareMs = v.GetInt32();
        if (Has("zeroMs", out v)) o.ZeroMs = v.GetInt32();
        if (Has("stabilityTimeoutMs", out v)) o.StabilityTimeoutMs = v.GetInt32();
        if (Has("continuousIntervalMs", out v)) o.ContinuousIntervalMs = v.GetInt32();
        if (Has("unavailable", out v)) o.Unavailable = v.GetBoolean();
        if (Has("extra", out v) && v.ValueKind == JsonValueKind.Object)
            foreach (var kv in v.EnumerateObject()) o.Extra[kv.Name] = kv.Value.ToString();
    }
}
