using System.Windows.Media.Media3D;
using EMFieldVisualizer.Physics;

namespace EMFieldVisualizer.Visualization;

/// <summary>Chooses which vectors to draw as "electric" and "magnetic" arrows for a given view mode.</summary>
public static class FieldVectorSelector
{
    /// <summary>
    /// Returns the arrays to draw (null = draw nothing). <paramref name="scratch"/> receives the transformed
    /// vectors in the equivalent-field views and must hold at least <paramref name="count"/> elements.
    /// </summary>
    public static (Vector3D[]? Electric, Vector3D[]? Magnetic) Select(
        FieldView view, bool showElectric, bool showMagnetic,
        Vector3D[] e, Vector3D[] cb, int count, Vector3D[] scratch)
    {
        switch (view)
        {
            case FieldView.PureElectric:
                for (int i = 0; i < count; i++) scratch[i] = EquivalentElectric(e[i], cb[i]);
                return (scratch, null);
            case FieldView.PureMagnetic:
                for (int i = 0; i < count; i++) scratch[i] = EquivalentMagnetic(e[i], cb[i]);
                return (null, scratch);
            default:
                return (showElectric ? e : null, showMagnetic ? cb : null);
        }
    }

    /// <summary>E′ along E where the field is electric-dominated (E &gt; cB), otherwise zero.</summary>
    public static Vector3D EquivalentElectric(Vector3D e, Vector3D cb)
    {
        var fa = FieldAnalysis.Analyze(e, cb);
        return fa.ElectricDominated && e.LengthSquared > 0 ? e / e.Length * fa.EPrime : default;
    }

    /// <summary>cB′ along B where the field is magnetic-dominated (cB &gt; E), otherwise zero.</summary>
    public static Vector3D EquivalentMagnetic(Vector3D e, Vector3D cb)
    {
        var fa = FieldAnalysis.Analyze(e, cb);
        return !fa.ElectricDominated && cb.LengthSquared > 0 ? cb / cb.Length * fa.CBPrime : default;
    }
}
