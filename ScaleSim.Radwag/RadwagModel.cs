using ScaleSim.Core;
using ScaleSim.Core.Abstractions;

namespace ScaleSim.Radwag;

/// <summary>
/// Radwag indicators/scales speaking the "CBCP-03" protocol over TCP (PUE 7.1, PUE HY10, WLY, WPY).
/// Reference: Radwag-PUE71-Commands.md. Where the manual and the original ESP32 sketch differ in
/// cosmetic details (the "+" sign, the "*" after the unit) the defaults follow the sketch, since
/// that is what the client software was written against; both are switchable in the scale's settings.
/// </summary>
public sealed class RadwagModel : IScaleModel
{
    public const string ModelId = "radwag-pue71";
    public const string PositiveSignKey = "positiveSign";
    public const string UnitMarkerKey = "unitMarker";

    public ScaleModelInfo Info { get; } = new(
        ModelId, "Radwag", "PUE 7.1 / HY10 (CBCP-03)", 4001,
        "Indicador Radwag por Ethernet. Comandos Z, T, OT, UT, S, SI, SIA, SU, SUI, C1/C0, CU1/CU0, DH/UH, ODH/OUH, SS, PN, NB, SM, RM, BP, OMI/OMS/OMG, PC.");

    public IReadOnlyList<OptionDefinition> ExtraOptions { get; } = new[]
    {
        new OptionDefinition(PositiveSignKey, "Signo de valores positivos", "choice", "+", new[] { "+", "espacio" },
            "El manual indica espacio; el sketch del ESP32 enviaba '+'."),
        new OptionDefinition(UnitMarkerKey, "Marcar la unidad con '*'", "bool", "true",
            Help: "Formato del sketch del ESP32 (por ejemplo \"kg*\"). Desactívalo para el formato literal del manual."),
    };

    public IReadOnlyList<ScaleMode> Modes { get; } = new[]
    {
        new ScaleMode(1, "Pesaje"),
        new ScaleMode(2, "Calculo de piezas"),
        new ScaleMode(3, "Desviaciones"),
        new ScaleMode(4, "Dosificacion"),
        new ScaleMode(11, "Suma"),
        new ScaleMode(12, "Controlador de peso"),
        new ScaleMode(13, "Estadisticas"),
    };

    public ScaleOptions CreateDefaultOptions()
    {
        var o = new ScaleOptions { Name = "Radwag PUE 7.1", Port = Info.DefaultPort };
        foreach (var d in ExtraOptions) o.Extra[d.Key] = d.Default;
        return o;
    }

    public IScaleProtocol CreateProtocol(ScaleContext context) => new RadwagProtocol(context, Modes);
}
