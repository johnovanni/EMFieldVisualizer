using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using EMFieldVisualizer.Physics;

namespace EMFieldVisualizer.Rendering;

/// <summary>
/// Draws each particle as a sphere Lorentz-contracted along x (by its γ in the observer's frame), with a
/// "clock hand" that spins at the particle's proper-time rate, plus a translucent guide tube per stream with
/// arrows showing which way each line moves in the observer's frame.
/// </summary>
public sealed class ParticleRenderer
{
    public const double ParticleRadius = 0.45;     // nm (display only)
    public const double ClockRevsPerSecond = 0.25;

    private readonly GeometryModel3D _electronModel;
    private readonly GeometryModel3D _protonModel;
    private readonly GeometryModel3D _handModel;
    private readonly List<ParticleVisual> _visuals = [];
    private string _signature = "";

    private sealed record ParticleVisual(ChargeLine Line, int Index, MatrixTransform3D Transform);

    public ParticleRenderer()
    {
        var sphere = new MeshBuilder();
        sphere.AddSphere(new Point3D(), 1.0, 14, 9);
        var sphereMesh = sphere.ToMesh();
        _electronModel = Frozen(sphereMesh, MeshBuilder.SolidMaterial(SceneStyle.Electron, 0.35));
        _protonModel = Frozen(sphereMesh, MeshBuilder.SolidMaterial(SceneStyle.Proton, 0.35));

        var hand = new MeshBuilder();
        hand.AddBox(new Point3D(-0.12, 0, -0.12), new Point3D(0.12, 1.45, 0.12));
        _handModel = Frozen(hand.ToMesh(), MeshBuilder.SolidMaterial(Colors.White, 0.5, specular: false));
    }

    /// <summary>Opaque particle models.</summary>
    public Model3DGroup Particles { get; } = new();

    /// <summary>Translucent stream guides; add after all opaque content so blending works.</summary>
    public Model3DGroup Guides { get; } = new();

    public int ParticleCount => _visuals.Count;

    public void Update(Simulation sim, bool showParticles)
    {
        RebuildIfNeeded(sim, showParticles);
        foreach (var pv in _visuals)
            pv.Transform.Matrix = ParticleMatrix(pv.Line, sim.ParticleX(pv.Line, pv.Index), pv.Index, sim.Relativistic);
    }

    /// <summary>Everything that requires the models to be recreated (as opposed to just moved).</summary>
    public static string StructureSignature(IEnumerable<ChargeLine> lines, bool showParticles)
    {
        var sb = new StringBuilder();
        sb.Append(showParticles);
        foreach (var l in lines)
            sb.Append($"|{l.Stream.GetHashCode()},{l.Role},{(int)l.Stream.Kind},{l.Count},{l.OffsetY},{l.OffsetZ},{Math.Sign(Math.Round(l.Beta, 9))}");
        return sb.ToString();
    }

    /// <summary>Scale (contracted by 1/γ along x) × clock-hand rotation about x × translation.</summary>
    public static Matrix3D ParticleMatrix(ChargeLine line, double x, int index, bool relativistic)
    {
        double gamma = relativistic ? line.Gamma : 1.0;
        double angle = (line.ProperTime * ClockRevsPerSecond * 360 + index * 37) % 360;
        var m = Matrix3D.Identity;
        m.Scale(new Vector3D(ParticleRadius / gamma, ParticleRadius, ParticleRadius));
        m.Rotate(new Quaternion(new Vector3D(1, 0, 0), angle));
        m.Translate(new Vector3D(x, line.OffsetY, line.OffsetZ));
        return m;
    }

    private void RebuildIfNeeded(Simulation sim, bool showParticles)
    {
        string sig = StructureSignature(sim.Lines, showParticles);
        if (sig == _signature) return;
        _signature = sig;

        Particles.Children.Clear();
        Guides.Children.Clear();
        _visuals.Clear();

        foreach (var group in sim.Lines.GroupBy(l => l.Stream))
            Guides.Children.Add(GuideModel(group.Key, group));

        if (!showParticles) return;
        foreach (var line in sim.Lines)
        {
            var body = line.Species == ParticleKind.Electron ? _electronModel : _protonModel;
            for (int i = 0; i < line.Count; i++)
            {
                var transform = new MatrixTransform3D();
                var g = new Model3DGroup { Transform = transform };
                g.Children.Add(body);
                g.Children.Add(_handModel);
                Particles.Children.Add(g);
                _visuals.Add(new ParticleVisual(line, i, transform));
            }
        }
    }

    private static GeometryModel3D GuideModel(ParticleStream s, IEnumerable<ChargeLine> lines)
    {
        var color = SceneStyle.ForKind(s.Kind);
        double half = Simulation.Length / 2;
        var guide = new MeshBuilder();
        guide.AddCylinder(new Point3D(-half, s.OffsetY, s.OffsetZ), new Point3D(half, s.OffsetY, s.OffsetZ), s.IsWire ? 0.7 : 0.15, 14);

        // One arrow per moving line, pointing the way it moves for this observer; offset so a wire's two arrows don't overlap.
        double lift = 0;
        foreach (var line in lines)
        {
            if (Math.Abs(line.Beta) < 1e-9) continue;
            double dir = Math.Sign(line.Beta);
            guide.AddArrow(new Point3D(dir * (half + 0.5), s.OffsetY + lift, s.OffsetZ), new Vector3D(dir * 2.5, 0, 0), 0.25);
            lift += 1.0;
        }

        byte alpha = s.IsWire ? (byte)45 : (byte)70;
        var material = new MaterialGroup();
        material.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B))));
        material.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(40, color.R, color.G, color.B))));
        material.Freeze();
        return SceneStyle.Model(guide.ToMesh(), material);
    }

    private static GeometryModel3D Frozen(MeshGeometry3D mesh, Material material)
    {
        var model = SceneStyle.Model(mesh, material);
        model.Freeze();
        return model;
    }
}
