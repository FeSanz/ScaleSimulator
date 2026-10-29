using ScaleSim.Core.Abstractions;
using ScaleSim.Core.Networking;

namespace ScaleSim.Core;

public enum ServerStatus { Stopped, Listening, Error }

/// <summary>One registered, running simulated scale: model + settings + physical state + TCP server.</summary>
public sealed class ScaleInstance
{
    public ScaleInstance(string id, IScaleModel model, ScaleOptions options, ScaleStateData? state = null)
    {
        Id = id;
        Model = model;
        Options = options;
        State = new ScaleState(options);
        State.Import(state);
        Log = new ActivityLog();
        Server = new TcpScaleServer(model.CreateProtocol(new ScaleContext(State, options, Log, ms => Beeped?.Invoke(ms))), Log);
        State.Changed += () => Changed?.Invoke(this);
        Server.ClientsChanged += () => Changed?.Invoke(this);
    }

    public string Id { get; }
    public IScaleModel Model { get; }
    public ScaleOptions Options { get; }
    public ScaleState State { get; }
    public ActivityLog Log { get; }
    public TcpScaleServer Server { get; }
    public ServerStatus Status { get; private set; } = ServerStatus.Stopped;
    public string? StatusMessage { get; private set; }

    /// <summary>State, status or clients changed; may fire from any thread.</summary>
    public event Action<ScaleInstance>? Changed;
    /// <summary>A protocol asked for an audible signal (milliseconds).</summary>
    public event Action<int>? Beeped;

    public void Start()
    {
        var error = Server.Start(Options.Port);
        Status = error is null ? ServerStatus.Listening : ServerStatus.Error;
        StatusMessage = error;
        Changed?.Invoke(this);
    }

    public void Stop()
    {
        Server.Stop();
        Status = ServerStatus.Stopped;
        StatusMessage = null;
        Changed?.Invoke(this);
    }

    /// <summary>Applies new settings. Returns an error message when they are invalid (nothing changes then).</summary>
    public string? ApplyOptions(ScaleOptions incoming)
    {
        var error = incoming.Validate();
        if (error is not null) return error;

        var previousUnit = Options.BaseUnit;
        var portChanged = incoming.Port != Options.Port;
        Options.CopyFrom(incoming);
        State.OnOptionsChanged(previousUnit);
        if (portChanged && Status == ServerStatus.Listening) Start();
        else Changed?.Invoke(this);
        return null;
    }
}
