using EMFieldVisualizer.Physics;
using static EMFieldVisualizer.Tests.TestHelpers;

namespace EMFieldVisualizer.Tests.Physics;

public class LorentzForceTests
{
    private const double Q = Phys.ElementaryCharge;

    [Fact]
    public void StationaryCharge_FeelsOnlyElectricForce()
    {
        var f = LorentzForce.Compute(Q, new Vector3D(), new Vector3D(1e6, 0, 0), new Vector3D(0, 0, 1));
        AssertClose(new Vector3D(Q * 1e6, 0, 0), f.Electric, 1e-25);
        Assert.Equal(0, f.Magnetic.Length);
        AssertClose(f.Electric, f.Total, 1e-25);
    }

    [Fact]
    public void MovingCharge_MagneticForceIsQVCrossB()
    {
        // v = 0.5c x̂, B = 1 T ẑ  →  v × B = −0.5c ŷ
        var f = LorentzForce.Compute(Q, new Vector3D(0.5, 0, 0), new Vector3D(), new Vector3D(0, 0, 1));
        AssertClose(new Vector3D(0, -0.5 * Phys.C * Q, 0), f.Magnetic, 1e-22);
    }

    [Fact]
    public void ZeroCharge_FeelsNoForce()
    {
        var f = LorentzForce.Compute(0, new Vector3D(0.5, 0, 0), new Vector3D(1, 2, 3), new Vector3D(1, 1, 1));
        Assert.Equal(0, f.Total.Length);
    }

    [Fact]
    public void MaxMagnitude_IsLargestOfTheThree()
    {
        var f = new ProbeForces(default, default, new Vector3D(3, 0, 0), new Vector3D(0, 4, 0));
        AssertClose(5, f.MaxMagnitude); // total
        var g = new ProbeForces(default, default, new Vector3D(3, 0, 0), new Vector3D(-2, 0, 0));
        AssertClose(3, g.MaxMagnitude); // electric
    }

    [Fact]
    public void ProbeOverload_UsesSimulationFieldAndProbeState()
    {
        var sim = SimulationWith(false, true, SingleParticle(ParticleKind.Proton, 0));
        var probe = new Probe { Position = new Point3D(-20, 5, 0), Charge = -2 };
        sim.FieldAt(probe.Position, out var e, out _);

        var f = LorentzForce.Compute(sim, probe);

        AssertClose(e * (-2 * Q), f.Electric, 1e-25);
        Assert.True(f.Electric.Y < 0); // attraction towards the proton
    }

    [Theory]
    [InlineData(0.3)]
    [InlineData(0.8)]
    public void CoMovingChargeNextToLine_SideForceIsReducedByOneOverGammaSquared(double beta)
    {
        // A probe riding alongside a proton beam at the same velocity: the magnetic attraction cancels a fraction β²
        // of the electric repulsion, so the transverse force is F_E / γ² — the lab-frame view of time dilation.
        var line = new ParticleStream { Kind = ParticleKind.Proton, Beta = beta, Count = 120 };
        var sim = SimulationWith(true, true, line);
        var probe = new Probe { Position = new Point3D(0, 5, 0), Charge = 1, BetaX = beta };

        var f = LorentzForce.Compute(sim, probe);

        AssertClose(1 - beta * beta, f.Total.Y / f.Electric.Y, 1e-9);
        Assert.True(f.Magnetic.Y < 0);
    }
}
