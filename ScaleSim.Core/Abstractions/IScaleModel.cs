namespace ScaleSim.Core.Abstractions;

/// <summary>Identity of a scale model in the catalog (what the "Alta de báscula" dialog lists).</summary>
public sealed record ScaleModelInfo(string Id, string Brand, string DisplayName, int DefaultPort, string Description);

/// <summary>A work mode the physical scale can be switched to (Radwag OMS/OMG, etc.).</summary>
public sealed record ScaleMode(int Id, string Name);

/// <summary>
/// A model-specific setting the UI renders generically. Keeps brand quirks (frame sign, unit
/// marker, ...) out of the common settings and out of the UI code: a new brand only declares them.
/// Type is "bool", "text" or "choice".
/// </summary>
public sealed record OptionDefinition(string Key, string Label, string Type, string Default, string[]? Choices = null, string? Help = null);

/// <summary>
/// One entry of the catalog: everything needed to simulate one family of scales. To add a brand,
/// implement this plus an <see cref="IScaleProtocol"/> and register it in <see cref="ScaleCatalog"/>.
/// </summary>
public interface IScaleModel
{
    ScaleModelInfo Info { get; }
    IReadOnlyList<OptionDefinition> ExtraOptions { get; }
    IReadOnlyList<ScaleMode> Modes { get; }
    /// <summary>Starting values for a newly registered scale of this model.</summary>
    ScaleOptions CreateDefaultOptions();
    IScaleProtocol CreateProtocol(ScaleContext context);
}

/// <summary>The wire protocol of a model. One instance per simulated scale, one session per TCP client.</summary>
public interface IScaleProtocol
{
    IProtocolSession OpenSession(IClientChannel channel);
}

public interface IProtocolSession : IAsyncDisposable
{
    /// <summary>One received command line, terminator already stripped. Lines of a client are handled in order.</summary>
    Task HandleLineAsync(string line, CancellationToken ct);
}

/// <summary>Outgoing side of one connected client.</summary>
public interface IClientChannel
{
    string Id { get; }
    string Remote { get; }
    /// <summary>Sends text as-is (the protocol adds its own terminators). <paramref name="continuous"/> only marks the log entry.</summary>
    Task SendAsync(string text, bool continuous = false);
}
