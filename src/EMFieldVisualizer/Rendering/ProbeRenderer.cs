using System.Windows.Media.Media3D;
using EMFieldVisualizer.Physics;
using EMFieldVisualizer.Visualization;

namespace EMFieldVisualizer.Rendering;

/// <summary>The probe charge in the main scene: a sphere coloured by sign plus its force arrows.</summary>
public sealed class ProbeRenderer
{
    public const double ArrowLength = 6.0; // nm, for the largest force

    private readonly GeometryModel3D _sphere;
    private readonly TranslateTransform3D _translate = new();
    private readonly ForceArrowSet _arrows = new(0.12);

    public ProbeRenderer()
    {
        var mesh = new MeshBuilder();
        mesh.AddSphere(new Point3D(), 0.7, 20, 12);
        _sphere = SceneStyle.Model(mesh.ToMesh(), SceneStyle.PositiveProbe);
        _sphere.Transform = _translate;
        Group.Children.Add(_sphere);
        Group.Children.Add(_arrows.Group);
    }

    public Model3DGroup Group { get; } = new();

    public void Update(Probe probe, ProbeForces forces, ForceScale scale, bool showForces)
    {
        var pos = probe.Position;
        _translate.OffsetX = pos.X;
        _translate.OffsetY = pos.Y;
        _translate.OffsetZ = pos.Z;
        var material = SceneStyle.ProbeMaterial(probe.Charge);
        if (_sphere.Material != material) _sphere.Material = _sphere.BackMaterial = material;
        _arrows.Update(pos, forces, forces.Beta, ArrowLength, scale, showForces);
    }
}
