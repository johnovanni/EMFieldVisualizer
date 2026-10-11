using System.Windows.Media;
using System.Windows.Media.Media3D;
using EMFieldVisualizer.Physics;
using EMFieldVisualizer.Visualization;

namespace EMFieldVisualizer.Rendering;

/// <summary>Four arrows from a common origin: electric force, magnetic force, total force and velocity.</summary>
public sealed class ForceArrowSet
{
    /// <summary>Velocity arrow length at |β| = 1, as a fraction of the maximum arrow length.</summary>
    public const double VelocityFraction = 0.65;

    private readonly double _radius;

    public ForceArrowSet(double radius)
    {
        _radius = radius;
        Group.Children.Add(Electric);
        Group.Children.Add(Magnetic);
        Group.Children.Add(Total);
        Group.Children.Add(Velocity);
    }

    public Model3DGroup Group { get; } = new();
    public GeometryModel3D Electric { get; } = Arrow(SceneStyle.ForceE);
    public GeometryModel3D Magnetic { get; } = Arrow(SceneStyle.ForceB);
    public GeometryModel3D Total { get; } = Arrow(SceneStyle.ForceTotal);
    public GeometryModel3D Velocity { get; } = Arrow(SceneStyle.Velocity);

    /// <summary>Redraws the arrows, sized by <paramref name="scale"/> up to <paramref name="maxLength"/>.</summary>
    public void Update(Point3D origin, ProbeForces forces, Vector3D beta, double maxLength, ForceScale scale, bool visible = true)
    {
        double largest = forces.MaxMagnitude;
        Electric.Geometry = visible ? ForceMesh(origin, forces.Electric, largest, maxLength, scale, _radius) : null;
        Magnetic.Geometry = visible ? ForceMesh(origin, forces.Magnetic, largest, maxLength, scale, _radius) : null;
        Total.Geometry = visible ? ForceMesh(origin, forces.Total, largest, maxLength, scale, _radius * 1.35) : null;
        Velocity.Geometry = visible ? Mesh(origin, beta * maxLength * VelocityFraction, _radius * 0.7, 0.5) : null;
    }

    private static MeshGeometry3D? ForceMesh(Point3D origin, Vector3D force, double largest, double maxLength, ForceScale scale, double radius)
    {
        double magnitude = force.Length;
        double length = scale.Length(magnitude, largest, maxLength);
        return length > 0 ? Mesh(origin, force / magnitude * length, radius, scale.ColorCoordinate(magnitude)) : null;
    }

    private static MeshGeometry3D? Mesh(Point3D start, Vector3D vector, double radius, double u)
    {
        if (vector.Length < 1e-6) return null;
        var mb = new MeshBuilder(32);
        mb.AddArrow(start, vector, radius, u);
        return mb.ToMesh();
    }

    /// <summary>Dim → base colour → bright, so strength also shows as brightness.</summary>
    private static GeometryModel3D Arrow(Color color)
    {
        static byte Mix(byte c, double target, double t) => (byte)Math.Round(c + (target - c) * t);
        var dim = Color.FromRgb(Mix(color.R, 40, 0.75), Mix(color.G, 40, 0.75), Mix(color.B, 40, 0.75));
        var bright = Color.FromRgb(Mix(color.R, 255, 0.5), Mix(color.G, 255, 0.5), Mix(color.B, 255, 0.5));
        return SceneStyle.Model(null, MeshBuilder.GradientMaterial(dim, color, bright));
    }
}
