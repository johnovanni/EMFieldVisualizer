namespace EMFieldVisualizer.Physics;

/// <summary>Which part of a <see cref="ParticleStream"/> a line represents.</summary>
public enum LineRole
{
    Beam,
    WireIons,
    WireElectrons,
}

/// <summary>
/// One periodic line of identical point charges, as seen by the current observer. A beam is one line; a neutral wire
/// is two (ions and electrons). Lab-frame properties come from the stream; observer-frame properties are recomputed by
/// <see cref="Simulation.Sync"/> whenever the observer's velocity changes.
/// </summary>
public sealed class ChargeLine
{
    internal ChargeLine(ParticleStream stream, LineRole role)
    {
        Stream = stream;
        Role = role;
    }

    public ParticleStream Stream { get; }
    public LineRole Role { get; }

    /// <summary>The particle type (a wire's lines are protons/ions and electrons).</summary>
    public ParticleKind Species => Role switch
    {
        LineRole.WireIons => ParticleKind.Proton,
        LineRole.WireElectrons => ParticleKind.Electron,
        _ => Stream.Kind,
    };

    public double ChargeSign => Species == ParticleKind.Electron ? -1.0 : 1.0;
    public double OffsetY => Stream.OffsetY;
    public double OffsetZ => Stream.OffsetZ;

    // ---- lab frame ----

    /// <summary>Signed velocity along x in the lab (c).</summary>
    public double LabBeta { get; internal set; }

    /// <summary>Particles per window in the lab (may be fractional with fixed proper spacing).</summary>
    public double LabCount { get; internal set; }

    // ---- observer frame ----

    /// <summary>Signed velocity along x in the observer's frame (c).</summary>
    public double Beta { get; internal set; }

    public double Gamma => Phys.Gamma(Math.Min(Math.Abs(Beta), 0.99999));

    /// <summary>Line density in the observer frame, as particles per window (before rounding).</summary>
    public double ExactCount { get; internal set; }

    /// <summary>Particles actually simulated and drawn (ExactCount rounded).</summary>
    public int Count { get; internal set; }

    /// <summary>
    /// Charge of each drawn particle (C). Exactly ±e for sparse lines; for dense lines it is scaled by
    /// ExactCount / Count so the line's charge density stays exact despite rounding to whole particles.
    /// </summary>
    public double ChargePerParticle { get; internal set; }

    // ---- animation state ----

    /// <summary>Position of the first particle as a fraction of the spacing.</summary>
    public double Phase { get; set; }

    /// <summary>Time shown by the particles' own clocks (display seconds).</summary>
    public double ProperTime { get; set; }

    /// <summary>True for the line whose clock represents the stream (the beam, or a wire's drifting electrons).</summary>
    public bool IsPrimary => Role != LineRole.WireIons;
}
