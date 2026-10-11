using System.Windows.Media.Media3D;

namespace EMFieldVisualizer.Visualization;

/// <summary>
/// Maps field magnitudes (which span many orders of magnitude) to arrow lengths and colours.
/// Lengths are √-compressed relative to a robust reference magnitude and capped; colour uses a log scale.
/// </summary>
public static class ArrowScaling
{
    /// <summary>Percentile of the non-zero magnitudes used as the reference ("typical") magnitude.</summary>
    public const double ReferencePercentile = 0.85;

    /// <summary>Arrows never grow beyond this multiple of the reference length.</summary>
    public const double MaxRelativeLength = 1.6;

    /// <summary>A reference-magnitude arrow is this fraction of a grid cell long (at scale 1).</summary>
    public const double CellFill = 0.85;

    /// <summary>Arrows shorter than this fraction of a cell are not drawn.</summary>
    public const double MinCellFraction = 0.04;

    public static double ReferenceMagnitude(IReadOnlyList<Vector3D>? vectors, int count)
    {
        if (vectors == null) return 0;
        var mags = new List<double>(count);
        for (int i = 0; i < count; i++)
        {
            double m = vectors[i].Length;
            if (m > 0) mags.Add(m);
        }
        if (mags.Count == 0) return 0;
        mags.Sort();
        return mags[(int)(ReferencePercentile * (mags.Count - 1))];
    }

    /// <summary>
    /// Each field's reference is at least this fraction of the field of a single line of the densest stream,
    /// so a field that has (almost) cancelled — like E around a neutral wire — is drawn tiny instead of being
    /// blown up to full size by per-field normalisation.
    /// </summary>
    public const double ReferenceFloorFraction = 0.05;

    public static double WithFloor(double reference, double singleLineField) =>
        Math.Max(reference, ReferenceFloorFraction * singleLineField);

    /// <summary>Eases the reference towards a new value to avoid flicker, but jumps on large changes.</summary>
    public static double Smooth(double previous, double target)
    {
        if (previous <= 0 || target <= 0) return target;
        double ratio = target / previous;
        if (ratio > 3 || ratio < 1 / 3.0) return target;
        return previous + (target - previous) * 0.15;
    }

    /// <summary>Arrow length in scene units, or 0 if the arrow is too short to draw.</summary>
    public static double Length(double magnitude, double reference, double cellSize, double userScale)
    {
        if (magnitude <= 0 || reference <= 0) return 0;
        double len = cellSize * CellFill * userScale * Math.Min(Math.Sqrt(magnitude / reference), MaxRelativeLength);
        return len < cellSize * MinCellFraction ? 0 : len;
    }

    /// <summary>Texture coordinate (0..1) selecting the colour along the gradient: weak → strong.</summary>
    public static double ColorCoordinate(double magnitude, double reference) =>
        magnitude <= 0 || reference <= 0 ? 0.02 : Math.Clamp((Math.Log10(magnitude / reference) + 1.2) / 1.8, 0.02, 0.98);
}
