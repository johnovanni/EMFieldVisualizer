using System.Windows.Media;
using System.Windows.Media.Media3D;
using EMFieldVisualizer.Physics;

namespace EMFieldVisualizer.Rendering;

/// <summary>Shared colours, materials and lighting for the 3D scenes.</summary>
public static class SceneStyle
{
    public static readonly Color Electron = Color.FromRgb(0x36, 0xE0, 0x7F);
    public static readonly Color Proton = Color.FromRgb(0xFF, 0x44, 0x66);
    public static readonly Color ForceE = Color.FromRgb(0xFF, 0xB0, 0x20);
    public static readonly Color ForceB = Color.FromRgb(0x20, 0xD0, 0xFF);
    public static readonly Color ForceTotal = Color.FromRgb(0xFF, 0xFF, 0xFF);
    public static readonly Color Velocity = Color.FromRgb(0x9C, 0xFF, 0x3A);
    public static readonly Color Axis = Color.FromRgb(0x50, 0x58, 0x68);
    public static readonly Color Wire = Color.FromRgb(0xFF, 0xC1, 0x4D);

    public static readonly Material ElectricField = MeshBuilder.GradientMaterial(
        Color.FromRgb(0x5A, 0x3A, 0x10), Color.FromRgb(0xFF, 0xB0, 0x20), Color.FromRgb(0xFF, 0x3A, 0x20));

    public static readonly Material MagneticField = MeshBuilder.GradientMaterial(
        Color.FromRgb(0x10, 0x3A, 0x5A), Color.FromRgb(0x20, 0xD0, 0xFF), Color.FromRgb(0xB0, 0x60, 0xFF));

    public static readonly Material PositiveProbe = MeshBuilder.SolidMaterial(Color.FromRgb(0xFF, 0x90, 0xA0), 0.4);
    public static readonly Material NegativeProbe = MeshBuilder.SolidMaterial(Color.FromRgb(0x90, 0xFF, 0xB0), 0.4);

    public static Color ForKind(ParticleKind kind) => kind switch
    {
        ParticleKind.Electron => Electron,
        ParticleKind.Proton => Proton,
        _ => Wire,
    };

    public static Material ProbeMaterial(double charge) => charge < 0 ? NegativeProbe : PositiveProbe;

    public static void AddLights(Model3DGroup group)
    {
        group.Children.Add(new AmbientLight(Color.FromRgb(0x50, 0x55, 0x60)));
        group.Children.Add(new DirectionalLight(Color.FromRgb(0xC8, 0xC8, 0xC8), new Vector3D(-0.5, -1, -0.7)));
        group.Children.Add(new DirectionalLight(Color.FromRgb(0x70, 0x70, 0x78), new Vector3D(0.6, 0.4, 0.8)));
    }

    /// <summary>A model with the same material on both faces.</summary>
    public static GeometryModel3D Model(MeshGeometry3D? mesh, Material material) =>
        new(mesh, material) { BackMaterial = material };
}
