using EMFieldVisualizer.Physics;
using static EMFieldVisualizer.Tests.TestHelpers;

namespace EMFieldVisualizer.Tests.Physics;

public class SimulationTests
{
    private const double Half = Simulation.Length / 2;
    private const double Soft2 = Simulation.Softening * Simulation.Softening;

    // ---------- particle bookkeeping ----------

    private static ChargeLine LineOf(Simulation sim, ParticleStream s) => sim.PrimaryLine(s)!;

    [Fact]
    public void Count_IsStreamCountWithoutContraction()
    {
        var s = new ParticleStream { Count = 20, Beta = 0.8 };
        var sim = new Simulation { ContractSpacing = false };
        sim.Sync([s]);
        Assert.Equal(20, LineOf(sim, s).Count);
    }

    [Fact]
    public void Count_ScalesByGammaWithFixedProperSpacing()
    {
        var s = new ParticleStream { Count = 20, Beta = 0.8 };
        var sim = new Simulation { ContractSpacing = true, Relativistic = true };
        sim.Sync([s]);
        // γ = 5/3 → 20 × 5/3 = 33.3 → 33 drawn
        Assert.Equal(33, LineOf(sim, s).Count);
        AssertClose(100.0 / 3.0, LineOf(sim, s).ExactCount);
    }

    [Fact]
    public void DenseLines_ScaleChargeToKeepExactDensity()
    {
        var s = new ParticleStream { Kind = ParticleKind.Electron, Count = 20, Beta = 0.8 };
        var sim = new Simulation { ContractSpacing = true };
        sim.Sync([s]);
        var line = LineOf(sim, s);
        AssertClose(-Phys.ElementaryCharge * (100.0 / 3.0) / 33, line.ChargePerParticle);
        AssertClose(-Phys.ElementaryCharge * 100.0 / 3.0, line.ChargePerParticle * line.Count);
    }

    [Fact]
    public void SparseLines_KeepWholeCharges()
    {
        var s = new ParticleStream { Kind = ParticleKind.Proton, Count = 3, Beta = 0.8 };
        var sim = new Simulation { ContractSpacing = true }; // 3 × 5/3 = 5 exactly... use an observer to get a fraction
        sim.ObserverBeta = 0.3;
        sim.Sync([s]);
        var line = LineOf(sim, s);
        Assert.True(line.ExactCount < Simulation.DensityScalingThreshold);
        Assert.Equal(Phys.ElementaryCharge, line.ChargePerParticle);
    }

    [Fact]
    public void LabLinesWithWholeCounts_CarryExactlyOneCharge()
    {
        var s = new ParticleStream { Kind = ParticleKind.Electron, Count = 50, Beta = 0.9 };
        var sim = new Simulation();
        sim.Sync([s]);
        Assert.Equal(-Phys.ElementaryCharge, LineOf(sim, s).ChargePerParticle);
    }

    [Fact]
    public void Count_IgnoresContractionInClassicalMode()
    {
        var s = new ParticleStream { Count = 20, Beta = 0.8 };
        var sim = new Simulation { ContractSpacing = true, Relativistic = false };
        sim.Sync([s]);
        Assert.Equal(20, LineOf(sim, s).Count);
    }

    [Fact]
    public void Count_IsCapped()
    {
        var s = new ParticleStream { Count = 200, Beta = 0.9999 };
        var sim = new Simulation { ContractSpacing = true };
        sim.Sync([s]);
        Assert.Equal(Simulation.MaxCount, LineOf(sim, s).Count);
    }

    [Fact]
    public void Sync_SkipsDisabledStreamsAndKeepsAnimationState()
    {
        var a = new ParticleStream();
        var b = new ParticleStream { Enabled = false };
        var sim = new Simulation();
        sim.Sync([a, b]);
        Assert.Single(sim.Lines);

        LineOf(sim, a).Phase = 0.3;
        sim.Sync([a, b]);
        Assert.Equal(0.3, LineOf(sim, a).Phase);
    }

    [Fact]
    public void ParticleX_SpreadsParticlesEvenlyInsideWindow()
    {
        var s = new ParticleStream { Count = 4 };
        var sim = new Simulation();
        sim.Sync([s]);
        var line = LineOf(sim, s);
        line.Phase = 0.25;
        var xs = Enumerable.Range(0, 4).Select(i => sim.ParticleX(line, i)).ToArray();

        Assert.All(xs, x => Assert.InRange(x, -Half, Half));
        AssertClose(-Half + 2.5, xs[0]);
        for (int i = 1; i < 4; i++) AssertClose(10.0, xs[i] - xs[i - 1]);
    }

    [Fact]
    public void ParticleX_WrapsAround()
    {
        var s = new ParticleStream { Count = 2 };
        var sim = new Simulation();
        sim.Sync([s]);
        var line = LineOf(sim, s);
        line.Phase = 0.5;
        // index 1 sits at (0.5 + 1) × 20 = 30 → x = 10; still inside
        AssertClose(10, sim.ParticleX(line, 1));
        line.Phase = 0.99;
        Assert.InRange(sim.ParticleX(line, 1), -Half, Half);
    }

    [Fact]
    public void Advance_MovesPhaseByDistanceOverSpacing()
    {
        var s = new ParticleStream { Count = 4, Beta = 0.5 }; // spacing 10 nm
        var sim = new Simulation();
        sim.Sync([s]);
        sim.Advance(dt: 1.0, unitsPerSecondAtC: 10); // moves 5 nm
        AssertClose(0.5, LineOf(sim, s).Phase);
    }

    [Fact]
    public void Advance_ReversedStreamWrapsPhaseIntoUnitInterval()
    {
        var s = new ParticleStream { Count = 4, Beta = 0.5, Reversed = true };
        var sim = new Simulation();
        sim.Sync([s]);
        sim.Advance(dt: 1.0, unitsPerSecondAtC: 10);
        AssertClose(0.5, LineOf(sim, s).Phase);
    }

    [Fact]
    public void Advance_ProperTimeIsDilatedByGamma()
    {
        var s = new ParticleStream { Beta = 0.6 };
        var sim = new Simulation { Relativistic = true };
        sim.Sync([s]);
        sim.Advance(dt: 2.0, unitsPerSecondAtC: 1);
        AssertClose(2.0 / 1.25, LineOf(sim, s).ProperTime);
    }

    [Fact]
    public void Advance_ClassicalProperTimeEqualsLabTime()
    {
        var s = new ParticleStream { Beta = 0.6 };
        var sim = new Simulation { Relativistic = false };
        sim.Sync([s]);
        sim.Advance(dt: 2.0, unitsPerSecondAtC: 1);
        AssertClose(2.0, LineOf(sim, s).ProperTime);
    }

    // ---------- fields ----------

    [Fact]
    public void StationaryProton_GivesSoftenedCoulombField()
    {
        var sim = SimulationWith(periodic: false, relativistic: true, SingleParticle(ParticleKind.Proton, 0));
        sim.FieldAt(new Point3D(-Half, 10, 0), out var e, out var b);

        double r2 = 100 + Soft2;
        double expectedEy = Phys.K * Phys.ElementaryCharge * 1e18 * 10 / (r2 * Math.Sqrt(r2));
        AssertClose(expectedEy, e.Y, 1e-12);
        AssertClose(0, e.X, absoluteTolerance: 1e-6);
        AssertClose(0, e.Z, absoluteTolerance: 1e-6);
        Assert.Equal(0, b.Length);
    }

    [Fact]
    public void Electron_FieldPointsTowardsIt()
    {
        var sim = SimulationWith(false, true, SingleParticle(ParticleKind.Electron, 0));
        sim.FieldAt(new Point3D(-Half, 5, 0), out var e, out _);
        Assert.True(e.Y < 0);
    }

    [Fact]
    public void MovingCharge_FieldIsEnhancedByGammaSideways()
    {
        const double beta = 0.8; // γ = 5/3
        var point = new Point3D(-Half, 10, 0);
        var rel = SimulationWith(false, true, SingleParticle(ParticleKind.Proton, beta));
        var cls = SimulationWith(false, false, SingleParticle(ParticleKind.Proton, beta));
        rel.FieldAt(point, out var eRel, out _);
        cls.FieldAt(point, out var eCls, out _);

        AssertClose(5.0 / 3.0, eRel.Length / eCls.Length, relativeTolerance: 1e-12);
    }

    [Fact]
    public void MovingCharge_FieldIsReducedByGammaSquaredAhead()
    {
        const double beta = 0.8;
        var ahead = new Point3D(-Half + 10, 0, 0); // on the line of motion: sin θ = 0 exactly
        var rel = SimulationWith(false, true, SingleParticle(ParticleKind.Proton, beta));
        var cls = SimulationWith(false, false, SingleParticle(ParticleKind.Proton, beta));
        rel.FieldAt(ahead, out var eRel, out _);
        cls.FieldAt(ahead, out var eCls, out _);

        AssertClose(1 - beta * beta, eRel.Length / eCls.Length, 1e-12);
    }

    [Fact]
    public void MagneticField_IsVCrossEOverCSquared()
    {
        const double beta = 0.5;
        var sim = SimulationWith(false, true, SingleParticle(ParticleKind.Proton, beta));
        var point = new Point3D(-Half + 3, 4, -2);
        sim.FieldAt(point, out var e, out var b);

        var expected = Vector3D.CrossProduct(new Vector3D(beta * Phys.C, 0, 0), e) / (Phys.C * Phys.C);
        AssertClose(expected, b, absoluteTolerance: expected.Length * 1e-12);
        AssertClose(0, Vector3D.DotProduct(e, b), absoluteTolerance: e.Length * b.Length * 1e-12);
    }

    [Fact]
    public void MagneticField_CirclesAPositiveChargeByRightHandRule()
    {
        var sim = SimulationWith(false, true, SingleParticle(ParticleKind.Proton, 0.5));
        sim.FieldAt(new Point3D(-Half, 5, 0), out _, out var b); // above the +x-moving proton
        Assert.True(b.Z > 0);                                  // x̂ × ŷ = ẑ
    }

    [Fact]
    public void ReversedStream_FlipsMagneticField()
    {
        var forward = SingleParticle(ParticleKind.Proton, 0.5);
        var backward = SingleParticle(ParticleKind.Proton, 0.5);
        backward.Reversed = true;
        var point = new Point3D(-Half, 5, 0);
        SimulationWith(false, true, forward).FieldAt(point, out var e1, out var b1);
        SimulationWith(false, true, backward).FieldAt(point, out var e2, out var b2);
        AssertClose(e1, e2, e1.Length * 1e-12);
        AssertClose(-b1, b2, b1.Length * 1e-12);
    }

    [Fact]
    public void DisabledStreams_ProduceNoField()
    {
        var s = SingleParticle(ParticleKind.Proton, 0.5);
        s.Enabled = false;
        SimulationWith(false, true, s).FieldAt(new Point3D(0, 5, 0), out var e, out var b);
        Assert.Equal(0, e.Length);
        Assert.Equal(0, b.Length);
    }

    [Fact]
    public void Fields_Superpose()
    {
        var a = SingleParticle(ParticleKind.Proton, 0.3);
        var c = new ParticleStream { Kind = ParticleKind.Electron, Beta = 0.7, Count = 1, OffsetY = 4 };
        var point = new Point3D(-15, 2, 1);
        SimulationWith(false, true, a).FieldAt(point, out var ea, out var ba);
        SimulationWith(false, true, c).FieldAt(point, out var ec, out var bc);
        SimulationWith(false, true, a, c).FieldAt(point, out var e, out var b);
        AssertClose(ea + ec, e, e.Length * 1e-12);
        AssertClose(ba + bc, b, b.Length * 1e-12);
    }

    [Fact]
    public void Periodic_AddsImageCharges()
    {
        var s = SingleParticle(ParticleKind.Proton, 0);
        var point = new Point3D(0, 0, 0); // midway between the particle at −20 and its image at +20
        SimulationWith(false, true, s).FieldAt(point, out var single, out _);
        SimulationWith(true, true, s).FieldAt(point, out var periodic, out _);
        Assert.True(single.X > 0);
        Assert.True(Math.Abs(periodic.X) < single.X * 0.5);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.9)]
    public void DenseLineCharge_ObeysGaussLawAtAnySpeed(double beta)
    {
        // A dense periodic line of protons: E = 2kλ/r regardless of speed (λ measured in the lab),
        // and B = μ0 I / (2π r) = β E / c.
        const int count = 200;
        const double r = 5;
        var line = new ParticleStream { Kind = ParticleKind.Proton, Beta = beta, Count = count };
        var sim = SimulationWith(periodic: true, relativistic: true, line);
        sim.FieldAt(new Point3D(0, r, 0), out var e, out var b);

        double lambda = count * Phys.ElementaryCharge / (Simulation.Length * Phys.MetresPerUnit);
        double expected = 2 * Phys.K * lambda / (r * Phys.MetresPerUnit);
        // The analytic tails make the periodic line effectively infinite; what remains is the 0.25 nm softening,
        // which lowers a line's field by r²/(r² + a²) ≈ 0.25 % at 5 nm.
        AssertClose(expected, e.Y, relativeTolerance: 0.004);
        AssertClose(beta * e.Y / Phys.C, b.Z, relativeTolerance: 1e-9, absoluteTolerance: 1e-30);
    }
}
