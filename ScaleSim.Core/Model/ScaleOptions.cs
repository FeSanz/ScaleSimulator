namespace ScaleSim.Core;

/// <summary>User-editable settings of one simulated scale. Quantities are in <see cref="BaseUnit"/>.</summary>
public sealed class ScaleOptions
{
    public string Name { get; set; } = "Báscula";
    public int Port { get; set; } = 4001;
    /// <summary>The scale's calibration unit: what S, SI, OT, ... report.</summary>
    public string BaseUnit { get; set; } = "kg";
    public double MaxCapacity { get; set; } = 1500;
    public int Decimals { get; set; } = 2;
    public string SerialNumber { get; set; } = "123456";
    public int Platforms { get; set; } = 1;

    /// <summary>Minimum wait before a stable reading is answered (S/SU/SS). The ESP32 sketch used 500 ms.</summary>
    public int StabilizationMs { get; set; } = 500;
    /// <summary>Time between "started" and "done" of a tare. The ESP32 sketch used 1000 ms.</summary>
    public int TareMs { get; set; } = 1000;
    public int ZeroMs { get; set; } = 0;
    /// <summary>How long a command waits for stability before answering "_E".</summary>
    public int StabilityTimeoutMs { get; set; } = 5000;
    public int ContinuousIntervalMs { get; set; } = 200;
    /// <summary>Simulates a busy scale: commands answer "_I" (understood, not available now).</summary>
    public bool Unavailable { get; set; }

    /// <summary>Model-specific settings, declared by <c>IScaleModel.ExtraOptions</c>.</summary>
    public Dictionary<string, string> Extra { get; set; } = new();

    public ScaleOptions Clone()
    {
        var c = (ScaleOptions)MemberwiseClone();
        c.Extra = new Dictionary<string, string>(Extra);
        return c;
    }

    public void CopyFrom(ScaleOptions o)
    {
        Name = o.Name; Port = o.Port; BaseUnit = o.BaseUnit; MaxCapacity = o.MaxCapacity;
        Decimals = o.Decimals; SerialNumber = o.SerialNumber; Platforms = o.Platforms;
        StabilizationMs = o.StabilizationMs; TareMs = o.TareMs; ZeroMs = o.ZeroMs;
        StabilityTimeoutMs = o.StabilityTimeoutMs; ContinuousIntervalMs = o.ContinuousIntervalMs;
        Unavailable = o.Unavailable;
        Extra = new Dictionary<string, string>(o.Extra);
    }

    /// <summary>Returns an error message, or null when the settings are usable.</summary>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name)) return "El nombre no puede estar vacío.";
        if (Port is < 1 or > 65535) return "El puerto debe estar entre 1 y 65535.";
        if (!UnitCatalog.IsKnown(BaseUnit)) return $"Unidad desconocida: {BaseUnit}.";
        if (!(MaxCapacity > 0)) return "La capacidad máxima debe ser mayor que cero.";
        if (Decimals is < 0 or > 5) return "Los decimales deben estar entre 0 y 5.";
        if (Platforms is < 1 or > 4) return "Las plataformas deben ser de 1 a 4.";
        if (StabilizationMs < 0 || TareMs < 0 || ZeroMs < 0 || StabilityTimeoutMs < 0) return "Los tiempos no pueden ser negativos.";
        if (ContinuousIntervalMs < 20) return "El intervalo continuo mínimo es 20 ms.";
        return null;
    }
}
