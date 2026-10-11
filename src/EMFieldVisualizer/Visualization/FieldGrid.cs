using System.Windows.Media.Media3D;
using EMFieldVisualizer.Physics;

namespace EMFieldVisualizer.Visualization;

/// <summary>The sample points where field arrows are drawn: a 3D lattice or a plane through the probe.</summary>
public sealed class FieldGrid
{
    /// <summary>Minimum half-width (nm) of the sampled region across the streams.</summary>
    public const double MinExtent = 10;

    /// <summary>Margin (nm) kept around the outermost stream.</summary>
    public const double StreamMargin = 7;

    /// <summary>Fraction of the simulation window covered along x.</summary>
    public const double XCoverage = 0.95;

    private readonly List<Point3D> _points = [];

    public IReadOnlyList<Point3D> Points => _points;

    /// <summary>Spacing between neighbouring sample points (nm), used to size the arrows.</summary>
    public double CellSize { get; private set; } = 1;

    /// <summary>Half-width of the region in y and z (nm).</summary>
    public double Extent { get; private set; } = MinExtent;

    public double XHalfWidth => Simulation.Length / 2 * XCoverage;

    public static double ComputeExtent(IEnumerable<ParticleStream> streams)
    {
        double maxOffset = 0;
        foreach (var s in streams)
            if (s.Enabled) maxOffset = Math.Max(maxOffset, Math.Max(Math.Abs(s.OffsetY), Math.Abs(s.OffsetZ)));
        return Math.Max(MinExtent, maxOffset + StreamMargin);
    }

    public void Build(IEnumerable<ParticleStream> streams, SliceMode slice, int resolution, Point3D probe)
    {
        _points.Clear();
        int n = Math.Max(1, resolution);
        double extent = Extent = ComputeExtent(streams);
        double xr = XHalfWidth;

        switch (slice)
        {
            case SliceMode.Volume:
            {
                int nx = Math.Clamp((int)Math.Round(n * xr / extent), 3, 3 * n);
                CellSize = Math.Min(2 * xr / nx, 2 * extent / n);
                for (int i = 0; i < nx; i++)
                for (int j = 0; j < n; j++)
                for (int k = 0; k < n; k++)
                    _points.Add(new Point3D(Center(-xr, xr, nx, i), Center(-extent, extent, n, j), Center(-extent, extent, n, k)));
                break;
            }
            case SliceMode.YZThroughProbe:
            {
                int m = 2 * n + 1;
                CellSize = 2 * extent / m;
                for (int j = 0; j < m; j++)
                for (int k = 0; k < m; k++)
                    _points.Add(new Point3D(probe.X, Center(-extent, extent, m, j), Center(-extent, extent, m, k)));
                break;
            }
            case SliceMode.XYThroughProbe:
            case SliceMode.XZThroughProbe:
            {
                int m = 2 * n + 1;
                int nx = Math.Clamp((int)Math.Round(m * xr / extent), 3, 4 * n);
                CellSize = Math.Min(2 * xr / nx, 2 * extent / m);
                bool xy = slice == SliceMode.XYThroughProbe;
                for (int i = 0; i < nx; i++)
                for (int j = 0; j < m; j++)
                {
                    double x = Center(-xr, xr, nx, i), t = Center(-extent, extent, m, j);
                    _points.Add(xy ? new Point3D(x, t, probe.Z) : new Point3D(x, probe.Y, t));
                }
                break;
            }
        }
    }

    /// <summary>Centre of cell <paramref name="i"/> when [min, max] is split into <paramref name="count"/> cells.</summary>
    public static double Center(double min, double max, int count, int i) => min + (i + 0.5) * (max - min) / count;
}
