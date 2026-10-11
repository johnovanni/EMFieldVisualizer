using System.Windows.Media.Media3D;
using EMFieldVisualizer.Physics;
using EMFieldVisualizer.Visualization;

namespace EMFieldVisualizer.Rendering;

/// <summary>The small force diagram: the probe at the origin with its force arrows, viewed from the main camera's direction.</summary>
public sealed class ForceDiagramRenderer
{
    public const double ArrowLength = 2.2;
    public const double CameraDistance = 7.5;

    private readonly GeometryModel3D _probe;
    private readonly ForceArrowSet _arrows = new(0.05);

    public ForceDiagramRenderer()
    {
        SceneStyle.AddLights(Scene);

        var axes = new MeshBuilder();
        axes.AddArrow(new Point3D(-2.6, 0, 0), new Vector3D(5.2, 0, 0), 0.025);
        axes.AddCylinder(new Point3D(0, -2.0, 0), new Point3D(0, 2.0, 0), 0.01, 6);
        axes.AddCylinder(new Point3D(0, 0, -2.0), new Point3D(0, 0, 2.0), 0.01, 6);
        Scene.Children.Add(SceneStyle.Model(axes.ToMesh(), MeshBuilder.SolidMaterial(SceneStyle.Axis, 0.4, specular: false)));

        var probe = new MeshBuilder();
        probe.AddSphere(new Point3D(), 0.2, 20, 12);
        _probe = SceneStyle.Model(probe.ToMesh(), SceneStyle.PositiveProbe);
        Scene.Children.Add(_probe);
        Scene.Children.Add(_arrows.Group);
    }

    public Model3DGroup Scene { get; } = new();

    public void Update(Probe probe, ProbeForces forces, ForceScale scale)
    {
        var material = SceneStyle.ProbeMaterial(probe.Charge);
        if (_probe.Material != material) _probe.Material = _probe.BackMaterial = material;
        _arrows.Update(new Point3D(), forces, forces.Beta, ArrowLength, scale);
    }

    /// <summary>Looks at the origin along the main camera's viewing direction so both views share orientation.</summary>
    public static void SyncCamera(PerspectiveCamera camera, Vector3D forward)
    {
        camera.Position = new Point3D(-forward.X * CameraDistance, -forward.Y * CameraDistance, -forward.Z * CameraDistance);
        camera.LookDirection = forward * CameraDistance;
        camera.UpDirection = OrbitCamera.WorldUp;
    }
}
