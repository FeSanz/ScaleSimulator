using System.Globalization;

namespace ScaleSim.Core;

public static class UnitCatalog
{
    // Units per kilogram.
    private static readonly Dictionary<string, double> PerKg = new()
    {
        ["kg"] = 1, ["g"] = 1000, ["mg"] = 1_000_000, ["lb"] = 2.2046226218, ["oz"] = 35.27396195, ["ct"] = 5000, ["N"] = 9.80665,
    };

    public static IReadOnlyList<string> Names { get; } = PerKg.Keys.ToList();

    public static bool IsKnown(string unit) => PerKg.ContainsKey(unit);

    public static double Convert(double value, string from, string to) => value / PerKg[from] * PerKg[to];

    /// <summary>Decimals to show in <paramref name="unit"/> so the displayed resolution matches the base unit's.</summary>
    public static int Decimals(int baseDecimals, string baseUnit, string unit) =>
        Math.Max(0, baseDecimals - (int)Math.Round(Math.Log10(PerKg[unit] / PerKg[baseUnit])));

    public static string Format(double value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero).ToString("F" + decimals, CultureInfo.InvariantCulture);
}
