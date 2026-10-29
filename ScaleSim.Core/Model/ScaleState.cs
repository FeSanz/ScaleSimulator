namespace ScaleSim.Core;

public enum WeightStatus { Stable, Unstable, Over, Under }

public enum ZeroResult { Ok, OutOfRange }

public enum TareResult { Ok, BelowRange }

/// <summary>One platform's reading. <c>Net</c> already includes simulated noise when unstable.</summary>
public readonly record struct ScaleReading(double Gross, double Tare, double Net, WeightStatus Status);

/// <summary>Persisted part of <see cref="ScaleState"/>.</summary>
public sealed class ScaleStateData
{
    public double[] Loads { get; set; } = Array.Empty<double>();
    public double[] Zeros { get; set; } = Array.Empty<double>();
    public double[] Tares { get; set; } = Array.Empty<double>();
    public int ActivePlatform { get; set; }
    public string? Unit { get; set; }
    public int Mode { get; set; } = 1;
    public double PieceMass { get; set; }
    public double ReferenceMass { get; set; }
    public double LowThreshold { get; set; }
    public double HighThreshold { get; set; }
}

/// <summary>
/// The simulated physical scale: what is on each platform, zero/tare offsets, stability, current
/// unit and work mode. Protocol-agnostic; every protocol reads and drives the same state, and the
/// UI manipulates it (placing weight) from the other side. Quantities are in the base unit.
/// </summary>
public sealed class ScaleState
{
    private readonly object _gate = new();
    private readonly Random _rng = new();
    private readonly ScaleOptions _options;
    private double[] _load = new double[1], _zero = new double[1], _tare = new double[1];
    private int _active;
    private bool _stable = true;
    private string _unit;
    private int _mode = 1;
    private double _pieceMass, _referenceMass, _low, _high;

    public event Action? Changed;

    public ScaleState(ScaleOptions options)
    {
        _options = options;
        _unit = options.BaseUnit;
        Resize();
    }

    // ---- settings reflected into the state -------------------------------------------------

    /// <summary>Call after the options changed (platform count, base unit).</summary>
    public void OnOptionsChanged(string previousBaseUnit)
    {
        lock (_gate)
        {
            if (previousBaseUnit != _options.BaseUnit && UnitCatalog.IsKnown(previousBaseUnit))
            {
                double Conv(double v) => UnitCatalog.Convert(v, previousBaseUnit, _options.BaseUnit);
                for (var i = 0; i < _load.Length; i++) { _load[i] = Conv(_load[i]); _zero[i] = Conv(_zero[i]); _tare[i] = Conv(_tare[i]); }
                _pieceMass = Conv(_pieceMass); _referenceMass = Conv(_referenceMass); _low = Conv(_low); _high = Conv(_high);
                if (_unit == previousBaseUnit) _unit = _options.BaseUnit;
            }
            Resize();
        }
        Changed?.Invoke();
    }

    private void Resize()
    {
        var n = _options.Platforms;
        if (_load.Length != n)
        {
            Array.Resize(ref _load, n); Array.Resize(ref _zero, n); Array.Resize(ref _tare, n);
        }
        _active = Math.Clamp(_active, 0, n - 1);
        if (!UnitCatalog.IsKnown(_unit)) _unit = _options.BaseUnit;
    }

    private double Step => Math.Pow(10, -_options.Decimals);

    // ---- simple properties -----------------------------------------------------------------

    public int ActivePlatform { get { lock (_gate) return _active; } set { Mutate(() => _active = Math.Clamp(value, 0, _options.Platforms - 1)); } }
    public bool Stable { get { lock (_gate) return _stable; } set { Mutate(() => _stable = value); } }
    public int Mode { get { lock (_gate) return _mode; } set { Mutate(() => _mode = value); } }
    public double PieceMass { get { lock (_gate) return _pieceMass; } set { Mutate(() => _pieceMass = value); } }
    public double ReferenceMass { get { lock (_gate) return _referenceMass; } set { Mutate(() => _referenceMass = value); } }
    public double LowThreshold { get { lock (_gate) return _low; } set { Mutate(() => _low = value); } }
    public double HighThreshold { get { lock (_gate) return _high; } set { Mutate(() => _high = value); } }

    public string Unit
    {
        get { lock (_gate) return _unit; }
        set { if (UnitCatalog.IsKnown(value)) Mutate(() => _unit = value); }
    }

    private void Mutate(Action change)
    {
        lock (_gate) change();
        Changed?.Invoke();
    }

    // ---- loads, zero, tare -----------------------------------------------------------------

    public double GetLoad(int platform) { lock (_gate) return _load[Idx(platform)]; }

    public void SetLoad(int platform, double value) => Mutate(() => _load[Idx(platform)] = Math.Max(0, value));

    public void AddLoad(int platform, double delta) => Mutate(() =>
    {
        var i = Idx(platform);
        _load[i] = Math.Max(0, _load[i] + delta);
    });

    public double GetTare(int platform) { lock (_gate) return _tare[Idx(platform)]; }

    public void SetTare(int platform, double value) => Mutate(() => _tare[Idx(platform)] = Math.Max(0, value));

    private int Idx(int platform) => Math.Clamp(platform, 0, _load.Length - 1);

    public ZeroResult Zero(int platform)
    {
        ZeroResult result;
        lock (_gate)
        {
            var i = Idx(platform);
            var gross = _load[i] - _zero[i];
            if (Math.Abs(gross) > _options.MaxCapacity * 0.02) return ZeroResult.OutOfRange;
            _zero[i] += gross;
            _tare[i] = 0;
            result = ZeroResult.Ok;
        }
        Changed?.Invoke();
        return result;
    }

    public TareResult Tare(int platform)
    {
        lock (_gate)
        {
            var i = Idx(platform);
            var gross = _load[i] - _zero[i];
            if (gross <= 0) return TareResult.BelowRange;
            _tare[i] = gross;
        }
        Changed?.Invoke();
        return TareResult.Ok;
    }

    // ---- reading ---------------------------------------------------------------------------

    /// <summary>A reading as the scale would report it now. Unstable scales jitter a couple of resolution steps.</summary>
    public ScaleReading Read(int platform)
    {
        lock (_gate)
        {
            var i = Idx(platform);
            var gross = _load[i] - _zero[i];
            var net = gross - _tare[i];
            var status = WeightStatus.Stable;
            if (gross > _options.MaxCapacity + 9 * Step) status = WeightStatus.Over;
            else if (gross < -_options.MaxCapacity * 0.05) status = WeightStatus.Under;
            else if (!_stable) status = WeightStatus.Unstable;

            if (status == WeightStatus.Unstable)
                net += _rng.Next(-2, 3) * Step;
            return new ScaleReading(gross, _tare[i], net, status);
        }
    }

    // ---- unit helpers ----------------------------------------------------------------------

    public double ToCurrentUnit(double baseValue)
    {
        lock (_gate) return UnitCatalog.Convert(baseValue, _options.BaseUnit, _unit);
    }

    public int DecimalsFor(string unit) => UnitCatalog.Decimals(_options.Decimals, _options.BaseUnit, unit);

    // ---- persistence -----------------------------------------------------------------------

    public ScaleStateData Export()
    {
        lock (_gate)
            return new ScaleStateData
            {
                Loads = (double[])_load.Clone(), Zeros = (double[])_zero.Clone(), Tares = (double[])_tare.Clone(),
                ActivePlatform = _active, Unit = _unit, Mode = _mode, PieceMass = _pieceMass,
                ReferenceMass = _referenceMass, LowThreshold = _low, HighThreshold = _high,
            };
    }

    public void Import(ScaleStateData? d)
    {
        if (d is null) return;
        lock (_gate)
        {
            for (var i = 0; i < _load.Length; i++)
            {
                if (i < d.Loads.Length) _load[i] = d.Loads[i];
                if (i < d.Zeros.Length) _zero[i] = d.Zeros[i];
                if (i < d.Tares.Length) _tare[i] = d.Tares[i];
            }
            _active = Math.Clamp(d.ActivePlatform, 0, _load.Length - 1);
            if (d.Unit is not null && UnitCatalog.IsKnown(d.Unit)) _unit = d.Unit;
            _mode = d.Mode; _pieceMass = d.PieceMass; _referenceMass = d.ReferenceMass;
            _low = d.LowThreshold; _high = d.HighThreshold;
        }
    }
}
