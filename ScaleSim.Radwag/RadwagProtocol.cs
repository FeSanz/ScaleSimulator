using System.Globalization;
using System.Text;
using ScaleSim.Core;
using ScaleSim.Core.Abstractions;

namespace ScaleSim.Radwag;

internal sealed class RadwagProtocol : IScaleProtocol
{
    private readonly ScaleContext _ctx;
    private readonly IReadOnlyList<ScaleMode> _modes;

    public RadwagProtocol(ScaleContext ctx, IReadOnlyList<ScaleMode> modes)
    {
        _ctx = ctx;
        _modes = modes;
    }

    public IProtocolSession OpenSession(IClientChannel channel) => new RadwagSession(_ctx, _modes, channel);
}

/// <summary>One connected client's conversation. Commands are answered strictly in order.</summary>
internal sealed class RadwagSession : IProtocolSession
{
    private const string Crlf = "\r\n";
    private const string CommandList =
        "Z,T,S,SI,SU,SUI,C1,C0,CU1,CU0,DH,ODH,UH,OUH,OT,UT,SIA,SS,PC,P1,P2,P3,P4,NB,SM,RM,BP,OMI,OMS,OMG";

    private static readonly HashSet<string> NoArgCommands = new()
    {
        "Z", "T", "OT", "S", "SI", "SIA", "SU", "SUI", "C1", "C0", "CU1", "CU0",
        "ODH", "OUH", "SS", "P1", "P2", "P3", "P4", "NB", "OMI", "OMG", "PC",
    };
    private static readonly HashSet<string> ArgCommands = new() { "UT", "DH", "UH", "SM", "RM", "BP", "OMS" };

    private readonly ScaleContext _ctx;
    private readonly IReadOnlyList<ScaleMode> _modes;
    private readonly IClientChannel _channel;
    private CancellationTokenSource? _continuous;

    public RadwagSession(ScaleContext ctx, IReadOnlyList<ScaleMode> modes, IClientChannel channel)
    {
        _ctx = ctx;
        _modes = modes;
        _channel = channel;
    }

    private ScaleState State => _ctx.State;
    private ScaleOptions Options => _ctx.Options;

    public async Task HandleLineAsync(string line, CancellationToken ct)
    {
        var text = line.Trim();
        if (text.Length == 0) return;

        var space = text.IndexOf(' ');
        var cmd = (space < 0 ? text : text[..space]).ToUpperInvariant();
        var arg = space < 0 ? "" : text[(space + 1)..].Trim();

        var known = NoArgCommands.Contains(cmd) || ArgCommands.Contains(cmd);
        // Wrong arity (e.g. "S 5" or a bare "UT") is a malformed command.
        if (!known || (NoArgCommands.Contains(cmd) && arg.Length > 0))
        {
            await Send("ES");
            return;
        }

        if (Options.Unavailable && cmd != "PC")
        {
            await Send($"{cmd}_I");
            return;
        }

        switch (cmd)
        {
            case "Z": await ZeroAsync(ct); break;
            case "T": await TareAsync(ct); break;
            case "OT": await Send($"OT {Mass9(State.Read(State.ActivePlatform).Tare, Options.Decimals)} {Unit3(Options.BaseUnit)} "); break;
            case "UT": await SetNumber(arg, "UT", v => State.SetTare(State.ActivePlatform, v)); break;
            case "S": await StableFrameAsync("S", "S", currentUnit: false, ct); break;
            case "SU": await StableFrameAsync("SU", "SU", currentUnit: true, ct); break;
            case "SI": await Send(Frame("SI", State.ActivePlatform, currentUnit: false)); break;
            case "SUI": await Send(Frame("SUI", State.ActivePlatform, currentUnit: true)); break;
            case "SIA": await SendAllPlatforms(); break;
            case "C1": await StartContinuous("C1", "SI", currentUnit: false); break;
            case "CU1": await StartContinuous("CU1", "SUI", currentUnit: true); break;
            case "C0": StopContinuous(); await Send("C0_A"); break;
            case "CU0": StopContinuous(); await Send("CU0_A"); break;
            case "DH": await SetNumber(arg, "DH", v => State.LowThreshold = v); break;
            case "UH": await SetNumber(arg, "UH", v => State.HighThreshold = v); break;
            case "ODH": await Send($"DH {Mass9(State.LowThreshold, Options.Decimals)} {Unit3(Options.BaseUnit)} "); break;
            case "OUH": await Send($"UH {Mass9(State.HighThreshold, Options.Decimals)} {Unit3(Options.BaseUnit)} "); break;
            case "SS": await PrintAsync(ct); break;
            case "P1": case "P2": case "P3": case "P4": await SelectPlatform(cmd); break;
            case "NB": await Send($"NB_A_\"{Options.SerialNumber}\""); break;
            case "SM": await SetPieceMass(arg); break;
            case "RM": await SetReferenceMass(arg); break;
            case "BP": await BeepAsync(arg); break;
            case "OMI": await Send("OMI" + Crlf + string.Concat(_modes.Select(m => $"{m.Id}_{m.Name}{Crlf}")) + "OK"); break;
            case "OMS": await SetMode(arg); break;
            case "OMG": await CurrentMode(); break;
            case "PC": await Send($"PC_A_\"{CommandList}\""); break;
        }
    }

    public ValueTask DisposeAsync()
    {
        StopContinuous();
        return ValueTask.CompletedTask;
    }

    // ---- commands with a start/done conversation ---------------------------------------------

    private async Task ZeroAsync(CancellationToken ct)
    {
        await Send("Z_A");
        if (!await WaitStableAsync(Options.ZeroMs, ct)) { await Send("Z_E"); return; }
        await Send(State.Zero(State.ActivePlatform) == ZeroResult.Ok ? "Z_D" : "Z_^");
    }

    private async Task TareAsync(CancellationToken ct)
    {
        await Send("T_A");
        if (!await WaitStableAsync(Options.TareMs, ct)) { await Send("T_E"); return; }
        await Send(State.Tare(State.ActivePlatform) == TareResult.Ok ? "T_D" : "T_v");
    }

    private async Task StableFrameAsync(string cmd, string prefix, bool currentUnit, CancellationToken ct)
    {
        await Send($"{cmd}_A");
        if (!await WaitStableAsync(Options.StabilizationMs, ct)) { await Send($"{cmd}_E"); return; }
        await Send(Frame(prefix, State.ActivePlatform, currentUnit));
    }

    private async Task PrintAsync(CancellationToken ct)
    {
        await Send("SS_A");
        if (!await WaitStableAsync(Options.StabilizationMs, ct)) { await Send("SS_E"); return; }
        var r = State.Read(State.ActivePlatform);
        _ctx.Log.Add(ActivityKind.Info, _channel.Remote,
            $"SS: pesaje registrado {UnitCatalog.Format(r.Net, Options.Decimals)} {Options.BaseUnit}");
        await Send("SS_D");
    }

    /// <summary>Waits the minimum settling time, then until the scale is stable. False on timeout.</summary>
    private async Task<bool> WaitStableAsync(int settleMs, CancellationToken ct)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        if (settleMs > 0) await Task.Delay(settleMs, ct);
        while (!State.Stable)
        {
            if (clock.ElapsedMilliseconds >= Options.StabilityTimeoutMs) return false;
            await Task.Delay(50, ct);
        }
        return true;
    }

    // ---- continuous transmission -------------------------------------------------------------

    private async Task StartContinuous(string cmd, string prefix, bool currentUnit)
    {
        StopContinuous();
        await Send($"{cmd}_A");
        var cts = new CancellationTokenSource();
        _continuous = cts;
        _ = Task.Run(async () =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(Options.ContinuousIntervalMs, cts.Token);
                    await _channel.SendAsync(Frame(prefix, State.ActivePlatform, currentUnit) + Crlf, continuous: true);
                }
            }
            catch { /* stopped, or the client went away */ }
        });
    }

    private void StopContinuous()
    {
        var cts = Interlocked.Exchange(ref _continuous, null);
        if (cts is null) return;
        try { cts.Cancel(); cts.Dispose(); } catch { /* already disposed */ }
    }

    // ---- simple commands ---------------------------------------------------------------------

    private async Task SetNumber(string arg, string cmd, Action<double> apply)
    {
        if (!TryParseMass(arg, out var value)) { await Send("ES"); return; }
        apply(value);
        await Send($"{cmd}_OK");
    }

    private async Task SetPieceMass(string arg)
    {
        if (State.Mode != 2) { await Send("SM_I"); return; }
        await SetNumber(arg, "SM", v => State.PieceMass = v);
    }

    private async Task SetReferenceMass(string arg)
    {
        if (State.Mode != 3) { await Send("RM_I"); return; }
        await SetNumber(arg, "RM", v => State.ReferenceMass = v);
    }

    private async Task SelectPlatform(string cmd)
    {
        var n = cmd[1] - '0';
        if (n > Options.Platforms) { await Send("ES"); return; }
        State.ActivePlatform = n - 1;
        await Send($"{cmd}_OK");
    }

    private async Task BeepAsync(string arg)
    {
        if (!int.TryParse(arg, NumberStyles.None, CultureInfo.InvariantCulture, out var ms)) { await Send("ES"); return; }
        _ctx.Beep(Math.Clamp(ms, 50, 5000));
        await Send("BP_OK");
    }

    private async Task SetMode(string arg)
    {
        if (!int.TryParse(arg, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || _modes.All(m => m.Id != id))
        {
            await Send("OMS_E");
            return;
        }
        State.Mode = id;
        await Send("OMS_OK");
    }

    private async Task CurrentMode()
    {
        var mode = _modes.FirstOrDefault(m => m.Id == State.Mode) ?? _modes[0];
        await Send($"OMG_{mode.Id}_{mode.Name}");
    }

    private async Task SendAllPlatforms()
    {
        var sb = new StringBuilder();
        for (var p = 0; p < Options.Platforms; p++) sb.Append(Frame($"P{p + 1}", p, currentUnit: false)).Append(Crlf);
        await _channel.SendAsync(sb.ToString());
    }

    // ---- frame formatting --------------------------------------------------------------------

    private Task Send(string line) => _channel.SendAsync(line + Crlf);

    /// <summary>
    /// Mass frame: prefix (3 columns) + stability sign + space + sign + 9-column mass + space + 3-column
    /// unit + CRLF, e.g. "S    +     12.50 kg*".
    /// </summary>
    private string Frame(string prefix, int platform, bool currentUnit)
    {
        var r = State.Read(platform);
        var unit = currentUnit ? State.Unit : Options.BaseUnit;
        var decimals = currentUnit ? State.DecimalsFor(unit) : Options.Decimals;
        var value = currentUnit ? State.ToCurrentUnit(r.Net) : r.Net;
        var stability = r.Status switch
        {
            WeightStatus.Unstable => '?',
            WeightStatus.Over => '^',
            WeightStatus.Under => 'v',
            _ => ' ',
        };
        var negative = Math.Round(value, decimals, MidpointRounding.AwayFromZero) < 0;
        var positive = _ctx.Extra(RadwagModel.PositiveSignKey, "+") == "+" ? '+' : ' ';
        return $"{prefix,-3}{stability} {(negative ? '-' : positive)}{Mass9(value, decimals)} {Unit3(unit)}";
    }

    private static string Mass9(double value, int decimals) =>
        UnitCatalog.Format(Math.Abs(value), decimals).PadLeft(9);

    private string Unit3(string unit) =>
        unit.PadRight(2) + (_ctx.ExtraBool(RadwagModel.UnitMarkerKey, true) ? '*' : ' ');

    /// <summary>Mass argument: dot decimal separator, optional trailing unit that is ignored.</summary>
    private static bool TryParseMass(string arg, out double value)
    {
        value = 0;
        var token = arg.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (token is null || token.Contains(',')) return false;
        return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);
    }
}
