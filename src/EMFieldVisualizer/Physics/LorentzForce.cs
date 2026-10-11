using System.Windows.Media.Media3D;

namespace EMFieldVisualizer.Physics;

/// <summary>
/// Fields at the probe and the resulting Lorentz force, split into electric and magnetic parts.
/// <paramref name="Beta"/> is the probe's velocity in the frame these were computed in.
/// </summary>
public readonly record struct ProbeForces(Vector3D E, Vector3D B, Vector3D Electric, Vector3D Magnetic, Vector3D Beta = default)
{
    public Vector3D Total => Electric + Magnetic;

    /// <summary>Largest of the three force magnitudes (for scaling arrows).</summary>
    public double MaxMagnitude => Math.Max(Electric.Length, Math.Max(Magnetic.Length, Total.Length));
}

public static class LorentzForce
{
    /// <summary>F = q(E + v×B), with q in coulombs, β = v/c, E in V/m and B in tesla.</summary>
    public static ProbeForces Compute(double chargeCoulombs, Vector3D beta, Vector3D e, Vector3D b)
    {
        var v = beta * Phys.C;
        return new ProbeForces(e, b, e * chargeCoulombs, Vector3D.CrossProduct(v, b) * chargeCoulombs, beta);
    }

    /// <summary>Force on the probe in the simulation's observer frame (the probe's lab velocity is transformed).</summary>
    public static ProbeForces Compute(Simulation sim, Probe probe)
    {
        sim.FieldAt(probe.Position, out var e, out var b);
        return Compute(probe.Charge * Phys.ElementaryCharge, sim.ToObserverVelocity(probe.Beta), e, b);
    }
}

/// <summary>
/// Consistency check between frames: the transverse force found by the observer, converted back to the lab,
/// compared with the transverse force computed directly in the lab. Relativity says they agree; Galilean
/// transformations with Maxwell's fields do not.
/// </summary>
public readonly record struct FrameCheck(double ObserverBeta, bool Relativistic, Vector3D ProbeBetaInFrame,
    Vector3D FromObserver, Vector3D InLab)
{
    public static FrameCheck Compute(ProbeForces observer, ProbeForces lab, double observerBeta, bool relativistic) =>
        new(observerBeta, relativistic, observer.Beta,
            Lorentz.TransverseForceToLab(observer.Total, observer.Beta, observerBeta, relativistic),
            new Vector3D(0, lab.Total.Y, lab.Total.Z));

    /// <summary>|difference| relative to the larger of the two (0 when both vanish).</summary>
    public double RelativeDifference
    {
        get
        {
            double scale = Math.Max(FromObserver.Length, InLab.Length);
            return scale > 0 ? (FromObserver - InLab).Length / scale : 0;
        }
    }
}
