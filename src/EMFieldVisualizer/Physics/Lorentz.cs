using System.Windows.Media.Media3D;

namespace EMFieldVisualizer.Physics;

/// <summary>
/// Transformations to an observer moving with velocity β_o along +x (a "standard boost").
/// Every <c>relativistic: false</c> variant is the Galilean equivalent, used by the classical mode.
/// </summary>
public static class Lorentz
{
    /// <summary>x velocity of something moving at <paramref name="beta"/> (lab), seen by the observer.</summary>
    public static double VelocityAlongX(double beta, double observerBeta, bool relativistic = true) =>
        relativistic ? (beta - observerBeta) / (1 - beta * observerBeta) : beta - observerBeta;

    /// <summary>Full 3D velocity transformation (relativistic velocity addition).</summary>
    public static Vector3D Velocity(Vector3D beta, double observerBeta, bool relativistic = true)
    {
        if (!relativistic) return beta - new Vector3D(observerBeta, 0, 0);
        double d = 1 - beta.X * observerBeta;
        double g = Phys.Gamma(observerBeta);
        return new Vector3D((beta.X - observerBeta) / d, beta.Y / (g * d), beta.Z / (g * d));
    }

    /// <summary>
    /// Particles per window seen by the observer for a periodic line with lab density <paramref name="labCount"/>
    /// moving at <paramref name="labBeta"/>. Spacing is contracted by each frame's own γ, so the density scales by
    /// γ(observer-frame velocity) / γ(lab velocity). Galilean: unchanged.
    /// </summary>
    public static double LineCount(double labCount, double labBeta, double observerBeta, bool relativistic = true)
    {
        if (!relativistic) return labCount;
        double u = VelocityAlongX(labBeta, observerBeta);
        return labCount * Phys.Gamma(u) / Phys.Gamma(labBeta);
    }

    /// <summary>
    /// Field transformation: E′∥ = E∥, E′⊥ = γ(E + v×B)⊥, B′∥ = B∥, B′⊥ = γ(B − v×E/c²)⊥, with v = β_o c x̂.
    /// </summary>
    public static (Vector3D E, Vector3D B) Fields(Vector3D e, Vector3D b, double observerBeta)
    {
        double g = Phys.Gamma(observerBeta);
        var v = new Vector3D(observerBeta * Phys.C, 0, 0);
        var ePerp = g * (e + Vector3D.CrossProduct(v, b));
        var bPerp = g * (b - Vector3D.CrossProduct(v, e) / (Phys.C * Phys.C));
        return (new Vector3D(e.X, ePerp.Y, ePerp.Z), new Vector3D(b.X, bPerp.Y, bPerp.Z));
    }

    /// <summary>
    /// Converts the transverse (y, z) part of a force measured by the observer back to the lab:
    /// F⊥ = F′⊥ / (γ_o (1 + β_o v′ₓ)). The x component is returned as 0.
    /// Galilean: forces are the same in every frame.
    /// </summary>
    public static Vector3D TransverseForceToLab(Vector3D forceInFrame, Vector3D betaInFrame, double observerBeta,
        bool relativistic = true)
    {
        double f = relativistic ? 1 / (Phys.Gamma(observerBeta) * (1 + observerBeta * betaInFrame.X)) : 1;
        return new Vector3D(0, forceInFrame.Y * f, forceInFrame.Z * f);
    }
}
