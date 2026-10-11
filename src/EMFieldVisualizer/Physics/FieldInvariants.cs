using System.Windows.Media.Media3D;

namespace EMFieldVisualizer.Physics;

/// <summary>
/// Lorentz-invariant analysis of an (E, B) pair.
///
/// The two invariants are  F = E² − c²B²  and  G = c E·B.  At any point there is a local inertial frame
/// (moving with velocity u along E×B) in which E′ ∥ B′, with
///     E′² = (F + √(F² + 4G²)) / 2,     c²B′² = (−F + √(F² + 4G²)) / 2.
/// If G = 0 (E ⊥ B, as for parallel streams with equal speeds) one of these vanishes, so the field is
/// *purely electric* (F &gt; 0) or *purely magnetic* (F &lt; 0) in that frame. In the pure case E′ points along E
/// and B′ points along B.
/// </summary>
public readonly record struct FieldAnalysis(
    double Invariant,      // E² − c²B²  (V²/m²)
    double EPrime,         // V/m in the parallel frame
    double CBPrime,        // V/m in the parallel frame
    double BoostBeta,      // speed of that frame / c
    Vector3D BoostDirection,
    double Purity)         // 1 = exactly pure (G = 0); smaller = some residual of the other field
{
    public bool ElectricDominated => Invariant > 0;

    public static FieldAnalysis Analyze(Vector3D e, Vector3D cb)
    {
        double f = e.LengthSquared - cb.LengthSquared;
        double g = Vector3D.DotProduct(e, cb);
        double s = Math.Sqrt(f * f + 4 * g * g);
        double ep = Math.Sqrt(Math.Max(0, (f + s) / 2));
        double bp = Math.Sqrt(Math.Max(0, (-f + s) / 2));

        var poynting = Vector3D.CrossProduct(e, cb);
        double sum = e.LengthSquared + cb.LengthSquared;
        double w = sum > 0 ? poynting.Length / sum : 0;  // = β/(1+β²) for the parallel frame
        double beta = w < 1e-15 ? 0 : (1 - Math.Sqrt(Math.Max(0, 1 - 4 * w * w))) / (2 * w);
        if (poynting.LengthSquared > 0) poynting.Normalize();

        double big = Math.Max(ep, bp), small = Math.Min(ep, bp);
        double purity = big > 0 ? 1 - small / big : 1;
        return new FieldAnalysis(f, ep, bp, beta, poynting, purity);
    }
}
