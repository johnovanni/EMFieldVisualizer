using System.Windows.Media.Media3D;
using EMFieldVisualizer.Physics;
using EMFieldVisualizer.Visualization;

namespace EMFieldVisualizer.Rendering;

/// <summary>Samples the field on a <see cref="FieldGrid"/> and draws it as two arrow meshes (E and B).</summary>
public sealed class FieldArrowRenderer
{
    private readonly FieldGrid _grid = new();
    private Vector3D[] _e = [], _cb = [], _scratch = [];
    private double _refElectric, _refMagnetic;

    public GeometryModel3D ElectricArrows { get; } = SceneStyle.Model(null, SceneStyle.ElectricField);
    public GeometryModel3D MagneticArrows { get; } = SceneStyle.Model(null, SceneStyle.MagneticField);

    public int ArrowCount { get; private set; }

    public void Update(Simulation sim, MainViewModel vm)
    {
        _grid.Build(vm.Streams, vm.Slice, vm.GridResolution, vm.Probe.Position);
        var points = _grid.Points;
        int n = points.Count;
        if (_e.Length < n)
        {
            _e = new Vector3D[n];
            _cb = new Vector3D[n];
            _scratch = new Vector3D[n];
        }

        for (int i = 0; i < n; i++)
        {
            sim.FieldAt(points[i], out var e, out var b);
            _e[i] = e;
            _cb[i] = b * Phys.C; // cB has the same units (V/m) as E
        }

        var (electric, magnetic) = FieldVectorSelector.Select(vm.ViewMode, vm.ShowElectric, vm.ShowMagnetic, _e, _cb, n, _scratch);

        double refE = ArrowScaling.ReferenceMagnitude(electric, n);
        double refB = ArrowScaling.ReferenceMagnitude(magnetic, n);
        if (vm.CommonScale)
        {
            // Measure both against the same reference so E and cB can be compared directly.
            refE = refB = Math.Max(ArrowScaling.ReferenceMagnitude(_e, n), ArrowScaling.ReferenceMagnitude(_cb, n));
        }
        double singleLineField = ForceReference.Compute(sim) / Phys.ElementaryCharge;
        refE = ArrowScaling.WithFloor(refE, singleLineField);
        refB = ArrowScaling.WithFloor(refB, singleLineField);
        _refElectric = ArrowScaling.Smooth(_refElectric, refE);
        _refMagnetic = ArrowScaling.Smooth(_refMagnetic, refB);

        ArrowCount = 0;
        ElectricArrows.Geometry = BuildMesh(electric, n, _refElectric, vm.ArrowScale);
        MagneticArrows.Geometry = BuildMesh(magnetic, n, _refMagnetic, vm.ArrowScale);
    }

    private MeshGeometry3D? BuildMesh(Vector3D[]? vectors, int n, double reference, double userScale)
    {
        if (vectors == null || reference <= 0) return null;
        double cell = _grid.CellSize;
        var mb = new MeshBuilder(n * 20);
        for (int i = 0; i < n; i++)
        {
            double m = vectors[i].Length;
            double len = ArrowScaling.Length(m, reference, cell, userScale);
            if (len <= 0) continue;
            var dir = vectors[i] / m;
            mb.AddArrow(_grid.Points[i] - dir * (len / 2), dir * len, cell * 0.05, ArrowScaling.ColorCoordinate(m, reference));
            ArrowCount++;
        }
        return mb.VertexCount == 0 ? null : mb.ToMesh();
    }
}
