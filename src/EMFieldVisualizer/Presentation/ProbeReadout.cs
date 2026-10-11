using System.Text;
using System.Windows.Media.Media3D;
using EMFieldVisualizer.Physics;
using EMFieldVisualizer.Visualization;

namespace EMFieldVisualizer.Presentation;

/// <summary>Builds the multi-line numeric readout shown under the force diagram.</summary>
public static class ProbeReadout
{
    /// <param name="frame">When the observer is not the lab: the cross-check between frames.</param>
    public static string Format(Probe probe, ProbeForces forces, ForceScale? scale = null, FrameCheck? frame = null)
    {
        var sb = new StringBuilder();
        var beta = forces.Beta;
        sb.AppendLine($"q = {probe.Charge:+0;−0;0} e at ({probe.X:0.0}, {probe.Y:0.0}, {probe.Z:0.0}) nm, |v| = {beta.Length:0.###}c" +
                      (frame is null ? "" : " for this observer"));
        sb.AppendLine($"E   = {Vec(forces.E, "V/m")}");
        sb.AppendLine($"B   = {Vec(forces.B, "T")}");
        sb.AppendLine($"F_E = {Vec(forces.Electric, "N")}");
        sb.AppendLine($"F_B = {Vec(forces.Magnetic, "N")}");
        sb.AppendLine($"F   = {Vec(forces.Total, "N")}");
        if (forces.Electric.Length > 0)
            sb.AppendLine($"|F_B| / |F_E| = {forces.Magnetic.Length / forces.Electric.Length:0.####}");
        if (scale is { } s) sb.AppendLine(s.Describe());
        if (frame is { } f)
        {
            sb.AppendLine();
            sb.AppendLine(FrameCheckText(f));
        }
        sb.AppendLine();
        sb.Append(EquivalentFieldText(forces.E, forces.B));
        return sb.ToString();
    }

    /// <summary>Explains whether this observer's force agrees with the lab's once converted back.</summary>
    public static string FrameCheckText(FrameCheck f)
    {
        var sb = new StringBuilder();
        var v = f.ProbeBetaInFrame;
        sb.AppendLine($"Probe velocity for this observer: ({v.X:0.###}, {v.Y:0.###}, {v.Z:0.###})c");
        string conversion = f.Relativistic ? "F′⊥ / (γₒ(1 + βₒv′ₓ))" : "F′⊥ (Galilean: unchanged)";
        sb.AppendLine($"Sideways force converted to lab, {conversion}: {Phys.Eng(f.FromObserver.Length, "N")}");
        sb.AppendLine($"Sideways force computed directly in the lab: {Phys.Eng(f.InLab.Length, "N")}");
        double pct = f.RelativeDifference * 100;
        if (f.Relativistic)
            sb.Append(pct < 5
                ? $"→ Same force, different explanation (differ by {pct:0.#} %, from the discrete particles)."
                : $"→ Differ by {pct:0.#} %: with few particles the two snapshots differ; add particles to compare.");
        else
            sb.Append(pct < 1
                ? "→ Agree (no magnetic effects involved here)."
                : $"→ Disagree by {pct:0.#} %: Maxwell's fields + Galilean frames are inconsistent; relativity fixes this.");
        return sb.ToString();
    }

    /// <summary>Explains whether the field at a point is electric- or magnetic-dominated and its pure equivalent.</summary>
    public static string EquivalentFieldText(Vector3D e, Vector3D b)
    {
        if (e.LengthSquared == 0 && b.LengthSquared == 0) return "No field here.";

        var fa = FieldAnalysis.Analyze(e, b * Phys.C);
        var sb = new StringBuilder();
        sb.AppendLine($"Invariant E² − c²B² is {(fa.ElectricDominated ? "positive → electric-dominated" : "negative → magnetic-dominated")}.");
        string frame = fa.BoostBeta > 1e-6
            ? $"an observer moving at {fa.BoostBeta:0.####}c along ({fa.BoostDirection.X:0.##}, {fa.BoostDirection.Y:0.##}, {fa.BoostDirection.Z:0.##})"
            : "an observer at rest in the lab";
        sb.Append(fa.ElectricDominated
            ? $"Equivalent pure electric field E′ = {Phys.Eng(fa.EPrime, "V/m")}, seen by {frame}"
            : $"Equivalent pure magnetic field B′ = {Phys.Eng(fa.CBPrime / Phys.C, "T")}, seen by {frame}");
        if (fa.Purity < 0.999)
        {
            string residual = fa.ElectricDominated
                ? "B′ = " + Phys.Eng(fa.CBPrime / Phys.C, "T")
                : "E′ = " + Phys.Eng(fa.EPrime, "V/m");
            sb.Append($"  (E·B ≠ 0 here, so the best frame still has a residual {residual})");
        }
        sb.Append('.');
        return sb.ToString();
    }

    private static string Vec(Vector3D v, string unit) =>
        $"{Phys.Eng(v.Length, unit)}  ({v.X:0.##e+0}, {v.Y:0.##e+0}, {v.Z:0.##e+0})";
}
