using System.Windows.Media.Media3D;

namespace EMFieldVisualizer.Physics;

/// <summary>A test charge that can be positioned anywhere and given a velocity.</summary>
public sealed class Probe : ObservableObject
{
    private double _x;
    private double _y = 6;
    private double _z;
    private double _charge = 1;
    private double _betaX;
    private double _betaY;
    private double _betaZ;

    public double X { get => _x; set => Set(ref _x, value); }
    public double Y { get => _y; set => Set(ref _y, value); }
    public double Z { get => _z; set => Set(ref _z, value); }

    /// <summary>Charge in units of e.</summary>
    public double Charge { get => _charge; set => Set(ref _charge, value); }

    public double BetaX { get => _betaX; set => Set(ref _betaX, value); }
    public double BetaY { get => _betaY; set => Set(ref _betaY, value); }
    public double BetaZ { get => _betaZ; set => Set(ref _betaZ, value); }

    public Point3D Position
    {
        get => new(X, Y, Z);
        set { X = value.X; Y = value.Y; Z = value.Z; }
    }

    /// <summary>Velocity as a fraction of c, clamped so that |β| &lt; 1.</summary>
    public Vector3D Beta
    {
        get
        {
            var b = new Vector3D(BetaX, BetaY, BetaZ);
            double len = b.Length;
            if (len > 0.999) b *= 0.999 / len;
            return b;
        }
    }
}
