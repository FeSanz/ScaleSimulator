using System.Net.NetworkInformation;
using System.Net.Sockets;
using ScaleSim.Core;
using ScaleSim.Core.Abstractions;

namespace ScaleSim.App.Services;

/// <summary>Builds the JSON-friendly objects the page renders. All numbers are formatted here so the page never does math on weights.</summary>
internal static class UiModel
{
    public static object Init(ScaleCatalog catalog) => new
    {
        type = "init",
        version = AppInfo.Version,
        units = UnitCatalog.Names,
        ips = NetworkInfo.LocalAddresses(),
        models = catalog.Models.Select(m => new
        {
            id = m.Info.Id,
            brand = m.Info.Brand,
            name = m.Info.DisplayName,
            defaultPort = m.Info.DefaultPort,
            description = m.Info.Description,
            extraOptions = m.ExtraOptions,
            modes = m.Modes,
        }),
    };

    public static object Snapshot(ScaleInstance i)
    {
        var o = i.Options;
        var s = i.State;
        var active = s.ActivePlatform;
        var unit = s.Unit;
        var unitDecimals = s.DecimalsFor(unit);

        var platforms = Enumerable.Range(0, o.Platforms).Select(p =>
        {
            var r = s.Read(p);
            return new
            {
                load = s.GetLoad(p),
                loadText = UnitCatalog.Format(s.GetLoad(p), o.Decimals),
                gross = UnitCatalog.Format(r.Gross, o.Decimals),
                tare = UnitCatalog.Format(r.Tare, o.Decimals),
                status = r.Status.ToString().ToLowerInvariant(),
            };
        }).ToList();

        var reading = s.Read(active);
        var shown = s.ToCurrentUnit(reading.Net);

        return new
        {
            type = "snapshot",
            id = i.Id,
            modelId = i.Model.Info.Id,
            status = i.Status.ToString().ToLowerInvariant(),
            statusMessage = i.StatusMessage,
            clients = i.Server.Clients.Select(c => new { c.Id, c.Remote, since = c.Since.ToString("HH:mm:ss") }),
            options = new
            {
                name = o.Name, port = o.Port, baseUnit = o.BaseUnit, maxCapacity = o.MaxCapacity, decimals = o.Decimals,
                serialNumber = o.SerialNumber, platforms = o.Platforms, stabilizationMs = o.StabilizationMs,
                tareMs = o.TareMs, zeroMs = o.ZeroMs, stabilityTimeoutMs = o.StabilityTimeoutMs,
                continuousIntervalMs = o.ContinuousIntervalMs, unavailable = o.Unavailable, extra = o.Extra,
            },
            state = new
            {
                platforms,
                active,
                stable = s.Stable,
                unit,
                mode = s.Mode,
                pieceMass = s.PieceMass,
                referenceMass = s.ReferenceMass,
                low = s.LowThreshold,
                high = s.HighThreshold,
            },
            display = new
            {
                value = UnitCatalog.Format(Math.Abs(shown), unitDecimals),
                negative = Math.Round(shown, unitDecimals, MidpointRounding.AwayFromZero) < 0,
                unit,
                status = reading.Status.ToString().ToLowerInvariant(),
                tared = reading.Tare > 0,
                gross = UnitCatalog.Format(s.ToCurrentUnit(reading.Gross), unitDecimals),
                tare = UnitCatalog.Format(s.ToCurrentUnit(reading.Tare), unitDecimals),
            },
        };
    }

    public static object Log(string id, IEnumerable<ActivityEntry> entries, bool reset = false) => new
    {
        type = "log",
        id,
        reset,
        entries = entries.Select(e => new
        {
            seq = e.Seq,
            time = e.Time.ToString("HH:mm:ss.fff"),
            kind = e.Kind.ToString().ToLowerInvariant(),
            client = e.Client,
            text = e.Text,
            cont = e.Continuous,
        }),
    };
}

internal static class NetworkInfo
{
    /// <summary>IPv4 addresses of connected, non-virtual-loopback interfaces: where a client on the LAN can reach the simulator.</summary>
    public static IReadOnlyList<object> LocalAddresses()
    {
        var result = new List<object>();
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                foreach (var ua in nic.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    var ip = ua.Address.ToString();
                    if (ip.StartsWith("169.254.")) continue;
                    result.Add(new { ip, name = nic.Name });
                }
            }
        }
        catch (NetworkInformationException) { }
        result.Add(new { ip = "127.0.0.1", name = "Esta PC (localhost)" });
        return result;
    }
}

internal static class AppInfo
{
    public static string Version =>
        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version is { } v ? $"{v.Major}.{v.Minor}.{v.Build}" : "1.0.0";
}
