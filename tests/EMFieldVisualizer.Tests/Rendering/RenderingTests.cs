using EMFieldVisualizer.Physics;
using EMFieldVisualizer.Rendering;
using EMFieldVisualizer.Visualization;
using static EMFieldVisualizer.Tests.TestHelpers;

namespace EMFieldVisualizer.Tests.Rendering;

public class MeshBuilderTests
{
    [Fact]
    public void Arrow_HasExpectedTopology()
    {
        var mb = new MeshBuilder();
        mb.AddArrow(new Point3D(), new Vector3D(0, 0, 2), 0.1, u: 0.7);
        var mesh = mb.ToMesh();

        Assert.Equal(20, mesh.Positions.Count);        // 12 shaft + 6 head ring + tip + back centre
        Assert.Equal(24 * 3, mesh.TriangleIndices.Count);
        Assert.All(mesh.TextureCoordinates, t => Assert.Equal(0.7, t.X));
        Assert.True(mesh.IsFrozen);
    }

    [Fact]
    public void Arrow_SpansStartToTip()
    {
        var mb = new MeshBuilder();
        mb.AddArrow(new Point3D(1, 1, 1), new Vector3D(3, 0, 0), 0.1);
        var mesh = mb.ToMesh();
        Assert.Equal(1.0, mesh.Positions.Min(p => p.X), 9);
        Assert.Equal(4.0, mesh.Positions.Max(p => p.X), 9);
    }

    [Fact]
    public void ZeroLengthArrow_IsSkipped()
    {
        var mb = new MeshBuilder();
        mb.AddArrow(new Point3D(), new Vector3D(), 0.1);
        Assert.Equal(0, mb.VertexCount);
    }

    [Fact]
    public void Sphere_HasUnitNormalsAndRadius()
    {
        var mb = new MeshBuilder();
        mb.AddSphere(new Point3D(1, 0, 0), 2, slices: 4, stacks: 2);
        var mesh = mb.ToMesh();

        Assert.Equal(3 * 5, mesh.Positions.Count);
        Assert.Equal(mesh.Positions.Count, mesh.Normals.Count);
        Assert.All(mesh.Normals, n => AssertClose(1, n.Length));
        Assert.All(mesh.Positions, p => AssertClose(2, (p - new Point3D(1, 0, 0)).Length));
    }

    [Fact]
    public void Box_HasSixQuads()
    {
        var mb = new MeshBuilder();
        mb.AddBox(new Point3D(), new Point3D(1, 2, 3));
        var mesh = mb.ToMesh();
        Assert.Equal(24, mesh.Positions.Count);
        Assert.Equal(36, mesh.TriangleIndices.Count);
    }

    [Fact]
    public void Cylinder_UsesTwoVerticesPerSide()
    {
        var mb = new MeshBuilder();
        mb.AddCylinder(new Point3D(), new Point3D(0, 5, 0), 1, sides: 8);
        Assert.Equal(16, mb.VertexCount);
    }

    [Fact]
    public void Materials_AreFrozen()
    {
        Assert.True(MeshBuilder.SolidMaterial(System.Windows.Media.Colors.Red).IsFrozen);
        Assert.True(MeshBuilder.GradientMaterial(System.Windows.Media.Colors.Red, System.Windows.Media.Colors.Blue).IsFrozen);
    }
}

public class ParticleRendererTests
{
    private static ChargeLine Line(ParticleStream s, double observerBeta = 0)
    {
        var sim = new Simulation { ObserverBeta = observerBeta };
        sim.Sync([s]);
        return sim.PrimaryLine(s)!;
    }

    [Fact]
    public void ParticleMatrix_ContractsAlongMotionAndTranslates()
    {
        var line = Line(new ParticleStream { Beta = 0.6, OffsetY = 2, OffsetZ = -3 });
        var m = ParticleRenderer.ParticleMatrix(line, x: 5, index: 0, relativistic: true);

        var tip = m.Transform(new Point3D(1, 0, 0));
        AssertClose(new Point3D(5 + ParticleRenderer.ParticleRadius / 1.25, 2, -3), tip);
        AssertClose(new Point3D(5, 2, -3), m.Transform(new Point3D()));
    }

    [Fact]
    public void ParticleMatrix_ContractionFollowsObserver()
    {
        // Riding with the beam, its particles are at rest and round again.
        var line = Line(new ParticleStream { Beta = 0.6 }, observerBeta: 0.6);
        var m = ParticleRenderer.ParticleMatrix(line, 0, 0, relativistic: true);
        AssertClose(ParticleRenderer.ParticleRadius, m.Transform(new Point3D(1, 0, 0)).X);
    }

    [Fact]
    public void ParticleMatrix_ClassicalModeIsNotContracted()
    {
        var line = Line(new ParticleStream { Beta = 0.9 });
        var m = ParticleRenderer.ParticleMatrix(line, 0, 0, relativistic: false);
        AssertClose(ParticleRenderer.ParticleRadius, m.Transform(new Point3D(1, 0, 0)).X);
    }

    [Fact]
    public void StructureSignature_IgnoresMotionButTracksLayout()
    {
        var sim = new Simulation();
        var s = new ParticleStream { Count = 10, Beta = 0.6 };
        sim.Sync([s]);
        string before = ParticleRenderer.StructureSignature(sim.Lines, true);

        sim.Lines[0].Phase = 0.4;
        sim.Lines[0].ProperTime = 3;
        s.Beta = 0.3;
        sim.Sync([s]);
        Assert.Equal(before, ParticleRenderer.StructureSignature(sim.Lines, true));

        s.Count = 11;
        sim.Sync([s]);
        Assert.NotEqual(before, ParticleRenderer.StructureSignature(sim.Lines, true));
        Assert.NotEqual(ParticleRenderer.StructureSignature(sim.Lines, true), ParticleRenderer.StructureSignature(sim.Lines, false));

        s.Reversed = true; // direction arrows change
        string forward = ParticleRenderer.StructureSignature(sim.Lines, true);
        sim.Sync([s]);
        Assert.NotEqual(forward, ParticleRenderer.StructureSignature(sim.Lines, true));
    }

    [Fact]
    public void Update_CreatesOneVisualPerParticleAndOneGuidePerEnabledStream()
    {
        var sim = new Simulation();
        sim.Sync(
        [
            new() { Count = 5 },
            new() { Count = 7, Kind = ParticleKind.Proton, OffsetY = 4 },
            new() { Count = 9, Enabled = false },
            new() { Count = 3, Kind = ParticleKind.NeutralWire, OffsetY = -4 }, // 3 ions + 3 electrons
        ]);
        var r = new ParticleRenderer();

        r.Update(sim, showParticles: true);
        Assert.Equal(18, r.ParticleCount);
        Assert.Equal(18, r.Particles.Children.Count);
        Assert.Equal(3, r.Guides.Children.Count);

        r.Update(sim, showParticles: false);
        Assert.Equal(0, r.ParticleCount);
        Assert.Equal(3, r.Guides.Children.Count);
    }
}

public class FieldArrowRendererTests
{
    private static (FieldArrowRenderer Renderer, Simulation Sim, MainViewModel Vm) Setup()
    {
        var vm = new MainViewModel { GridResolution = 4 };
        var sim = new Simulation();
        sim.Prepare(vm.Streams);
        return (new FieldArrowRenderer(), sim, vm);
    }

    [Fact]
    public void LabFrame_DrawsBothFields()
    {
        var (r, sim, vm) = Setup();
        r.Update(sim, vm);
        Assert.NotNull(r.ElectricArrows.Geometry);
        Assert.NotNull(r.MagneticArrows.Geometry);
        Assert.True(r.ArrowCount > 0);
    }

    [Fact]
    public void HidingBothFields_DrawsNothing()
    {
        var (r, sim, vm) = Setup();
        vm.ShowElectric = false;
        vm.ShowMagnetic = false;
        r.Update(sim, vm);
        Assert.Null(r.ElectricArrows.Geometry);
        Assert.Null(r.MagneticArrows.Geometry);
        Assert.Equal(0, r.ArrowCount);
    }

    [Fact]
    public void StationaryCharges_HaveNoMagneticArrows()
    {
        var (r, sim, vm) = Setup();
        vm.SelectedStream!.Beta = 0;
        sim.Prepare(vm.Streams);
        r.Update(sim, vm);
        Assert.NotNull(r.ElectricArrows.Geometry);
        Assert.Null(r.MagneticArrows.Geometry);
    }

    [Fact]
    public void PureElectricView_UsesOnlyElectricArrows()
    {
        var (r, sim, vm) = Setup();
        vm.ViewModeIndex = (int)FieldView.PureElectric;
        r.Update(sim, vm);
        Assert.NotNull(r.ElectricArrows.Geometry);
        Assert.Null(r.MagneticArrows.Geometry);
    }
}

public class ForceRenderingTests
{
    private static double ArrowLength(GeometryModel3D model) =>
        model.Geometry is MeshGeometry3D mesh ? mesh.Positions.Max(p => (p - new Point3D()).Length) : 0;

    [Fact]
    public void Update_HidesArrowsWhenInvisibleOrZero()
    {
        var set = new ForceArrowSet(0.1);
        var forces = new ProbeForces(default, default, new Vector3D(1, 0, 0), new Vector3D());

        set.Update(new Point3D(), forces, new Vector3D(), 2, ForceScale.Normalized);
        Assert.NotNull(set.Electric.Geometry);
        Assert.NotNull(set.Total.Geometry);
        Assert.Null(set.Magnetic.Geometry); // zero force
        Assert.Null(set.Velocity.Geometry); // at rest

        set.Update(new Point3D(), forces, new Vector3D(0.5, 0, 0), 2, ForceScale.Normalized, visible: false);
        Assert.Null(set.Electric.Geometry);
        Assert.Null(set.Velocity.Geometry);
    }

    [Fact]
    public void Normalized_LargestArrowAlwaysFullLength()
    {
        var set = new ForceArrowSet(0.01);
        set.Update(new Point3D(), new ProbeForces(default, default, new Vector3D(1e-15, 0, 0), default), default, 2, ForceScale.Normalized);
        AssertClose(2, ArrowLength(set.Electric), 1e-9);
        set.Update(new Point3D(), new ProbeForces(default, default, new Vector3D(1e-9, 0, 0), default), default, 2, ForceScale.Normalized);
        AssertClose(2, ArrowLength(set.Electric), 1e-9);
    }

    [Fact]
    public void Absolute_StrongerForceDrawsLongerArrow()
    {
        var set = new ForceArrowSet(0.01);
        var scale = ForceScale.AbsoluteTo(1e-12);

        set.Update(new Point3D(), new ProbeForces(default, default, new Vector3D(0.25e-12, 0, 0), default), default, 2, scale);
        double weak = ArrowLength(set.Electric);
        set.Update(new Point3D(), new ProbeForces(default, default, new Vector3D(1e-12, 0, 0), default), default, 2, scale);
        double strong = ArrowLength(set.Electric);

        AssertClose(0.5, weak, 1e-9);   // 2 × 0.5 × √0.25
        AssertClose(1.0, strong, 1e-9); // reference → half of max length
    }

    [Fact]
    public void SyncCamera_LooksAtOriginAlongMainCameraDirection()
    {
        var pc = new PerspectiveCamera();
        var forward = new Vector3D(0, -0.6, -0.8);
        ForceDiagramRenderer.SyncCamera(pc, forward);
        AssertClose(new Point3D(0, 0, 0), pc.Position + pc.LookDirection);
        AssertClose(ForceDiagramRenderer.CameraDistance, pc.LookDirection.Length);
    }

    [Fact]
    public void ProbeMaterial_DependsOnSign()
    {
        Assert.Same(SceneStyle.NegativeProbe, SceneStyle.ProbeMaterial(-1));
        Assert.Same(SceneStyle.PositiveProbe, SceneStyle.ProbeMaterial(1));
    }
}
