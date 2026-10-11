using EMFieldVisualizer.Physics;

namespace EMFieldVisualizer.Visualization;

/// <summary>
/// How probe force arrows are sized.
/// <list type="bullet">
/// <item><b>Absolute</b>: length ∝ √(|F| / reference), clamped, so arrows grow and shrink as the probe moves.
/// The reference is fixed for a given set of streams, so all three force arrows share one scale.</item>
/// <item><b>Normalized</b>: the largest arrow always has full length; only direction and relative size are shown.</item>
/// </list>
/// </summary>
public readonly record struct ForceScale(bool Absolute, double Reference)
{
    /// <summary>A force equal to the reference is drawn at this fraction of the maximum length.</summary>
    public const double ReferenceFraction = 0.5;

    /// <summary>Visible arrows are never shorter than this fraction of the maximum length.</summary>
    public const double MinFraction = 0.06;

    /// <summary>Forces weaker than this fraction of the reference are not drawn at all.</summary>
    public const double HideBelowRatio = 1e-4;

    public static ForceScale Normalized => new(false, 0);

    public static ForceScale AbsoluteTo(double reference) => new(true, reference);

    private bool UsesReference => Absolute && Reference > 0;

    /// <summary>Arrow length for a force of <paramref name="magnitude"/>; <paramref name="largestInSet"/> is the biggest of the arrows drawn together.</summary>
    public double Length(double magnitude, double largestInSet, double maxLength)
    {
        if (magnitude <= 0) return 0;
        if (!UsesReference) return largestInSet > 0 ? maxLength * magnitude / largestInSet : 0;

        double ratio = magnitude / Reference;
        if (ratio < HideBelowRatio) return 0;
        return Math.Clamp(maxLength * ReferenceFraction * Math.Sqrt(ratio), maxLength * MinFraction, maxLength);
    }

    /// <summary>Texture coordinate: dim (weak) → bright (strong) in absolute mode, mid-tone when normalized.</summary>
    public double ColorCoordinate(double magnitude) =>
        UsesReference ? ArrowScaling.ColorCoordinate(magnitude, Reference) : 0.5;

    public string Describe() => UsesReference
        ? $"Arrows: √-scaled; half length = {Phys.Eng(Reference, "N")} (force on +1e at {ForceReference.Distance:0} nm from densest stream)"
        : "Arrows: normalized (direction and relative size only)";
}

/// <summary>Computes the fixed reference force used by <see cref="ForceScale"/> in absolute mode.</summary>
public static class ForceReference
{
    /// <summary>Distance (nm) from the stream at which the reference force is evaluated.</summary>
    public const double Distance = 5.0;

    /// <summary>
    /// Force on a +1e charge at rest <see cref="Distance"/> nm from the densest charge line (a neutral wire counts by
    /// its individual ion or electron line), treating it as a uniform line charge: F = e · 2kλ/r (exact for any speed
    /// by Gauss's law, with λ the density seen by the current observer). It depends only on the line densities, not on
    /// particle positions, so it does not flicker as particles move. Call after <see cref="Simulation.Sync"/>.
    /// </summary>
    public static double Compute(Simulation sim)
    {
        double densest = 0; // in elementary charges per window
        foreach (var line in sim.Lines)
            densest = Math.Max(densest, line.Count * Math.Abs(line.ChargePerParticle) / Phys.ElementaryCharge);
        if (densest == 0) return 0;

        double lambda = densest * Phys.ElementaryCharge / (Simulation.Length * Phys.MetresPerUnit);
        return Phys.ElementaryCharge * 2 * Phys.K * lambda / (Distance * Phys.MetresPerUnit);
    }
}
