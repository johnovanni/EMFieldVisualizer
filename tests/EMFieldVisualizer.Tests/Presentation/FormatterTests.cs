using EMFieldVisualizer.Physics;
using EMFieldVisualizer.Presentation;
using EMFieldVisualizer.Visualization;

namespace EMFieldVisualizer.Tests.Presentation;

public class ProbeReadoutTests : InvariantCultureTest
{
    [Fact]
    public void Format_ListsFieldsAndForces()
    {
        var probe = new Probe { Position = new Point3D(1, 2, 3), Charge = -1, BetaX = 0.5 };
        var forces = LorentzForce.Compute(-Phys.ElementaryCharge, probe.Beta, new Vector3D(0, 1e9, 0), new Vector3D(0, 0, 1));

        string text = ProbeReadout.Format(probe, forces);

        Assert.Contains("q = −1 e at (1.0, 2.0, 3.0) nm", text);
        Assert.Contains("|v| = 0.5c", text);
        Assert.Contains("E   = 1 GV/m", text);
        Assert.Contains("B   = 1 T", text);
        Assert.Contains("F_E =", text);
        Assert.Contains("F_B =", text);
        Assert.Contains("|F_B| / |F_E|", text);
    }

    [Fact]
    public void Format_DescribesArrowScaleWhenGiven()
    {
        var probe = new Probe();
        var forces = LorentzForce.Compute(Phys.ElementaryCharge, default, new Vector3D(1, 0, 0), default);
        Assert.DoesNotContain("Arrows:", ProbeReadout.Format(probe, forces));
        Assert.Contains("Arrows: normalized", ProbeReadout.Format(probe, forces, ForceScale.Normalized));
        Assert.Contains("Arrows: √-scaled", ProbeReadout.Format(probe, forces, ForceScale.AbsoluteTo(1e-12)));
    }

    [Fact]
    public void Format_IncludesFrameCheckWhenObserverMoves()
    {
        var probe = new Probe();
        var forces = LorentzForce.Compute(Phys.ElementaryCharge, default, new Vector3D(0, 1, 0), default);
        var check = new FrameCheck(0.5, true, new Vector3D(), new Vector3D(0, 1e-12, 0), new Vector3D(0, 1e-12, 0));
        string text = ProbeReadout.Format(probe, forces, frame: check);
        Assert.Contains("for this observer", text);
        Assert.Contains("Sideways force computed directly in the lab: 1 pN", text);
        Assert.DoesNotContain("for this observer", ProbeReadout.Format(probe, forces));
    }

    [Fact]
    public void FrameCheckText_RelativisticAgreement() =>
        Assert.Contains("Same force, different explanation",
            ProbeReadout.FrameCheckText(new FrameCheck(0.5, true, default, new Vector3D(0, 1.01, 0), new Vector3D(0, 1, 0))));

    [Fact]
    public void FrameCheckText_GalileanDisagreement() =>
        Assert.Contains("Galilean frames are inconsistent",
            ProbeReadout.FrameCheckText(new FrameCheck(0.5, false, default, new Vector3D(), new Vector3D(0, 1, 0))));

    [Fact]
    public void EquivalentFieldText_NoField() =>
        Assert.Equal("No field here.", ProbeReadout.EquivalentFieldText(new Vector3D(), new Vector3D()));

    [Fact]
    public void EquivalentFieldText_StaticElectricField()
    {
        string text = ProbeReadout.EquivalentFieldText(new Vector3D(1e6, 0, 0), new Vector3D());
        Assert.Contains("electric-dominated", text);
        Assert.Contains("E′ = 1 MV/m", text);
        Assert.Contains("at rest in the lab", text);
        Assert.DoesNotContain("residual", text);
    }

    [Fact]
    public void EquivalentFieldText_MagneticDominatedReportsBoost()
    {
        // E = 3, cB = 5 → B′ = 4/c, frame speed 0.6c
        string text = ProbeReadout.EquivalentFieldText(new Vector3D(3e8, 0, 0), new Vector3D(0, 5e8 / Phys.C, 0));
        Assert.Contains("magnetic-dominated", text);
        Assert.Contains("0.6c", text);
    }

    [Fact]
    public void EquivalentFieldText_ParallelFieldsMentionResidual()
    {
        string text = ProbeReadout.EquivalentFieldText(new Vector3D(3, 0, 0), new Vector3D(4 / Phys.C, 0, 0));
        Assert.Contains("residual", text);
    }
}

public class HudFormatterTests : InvariantCultureTest
{
    private static Simulation Synced(double observerBeta, params ParticleStream[] streams)
    {
        var sim = new Simulation { ObserverBeta = observerBeta };
        sim.Sync(streams);
        return sim;
    }

    [Fact]
    public void Status_ListsCountsModeObserverAndStreams()
    {
        ParticleStream[] streams =
        [
            new() { Kind = ParticleKind.Electron, Beta = 0.6 },
            new() { Kind = ParticleKind.Proton, Beta = 0.5, Reversed = true, Enabled = false },
        ];

        string text = HudFormatter.Status(59.6, 120, 40, "Lab frame: E and B (as measured)", Synced(0, streams), streams);

        Assert.StartsWith("60 fps   120 field arrows   40 particles", text);
        Assert.Contains("View: Lab frame: E and B   Physics: relativistic", text);
        Assert.Contains("Observer: lab frame", text);
        Assert.Contains("#1 e⁻ β=+0.6 γ=1.25", text);
        Assert.Contains("#2 p⁺  (off)", text);
        Assert.False(text.EndsWith('\n'));
    }

    [Fact]
    public void Status_ShowsObserverFrameVelocities()
    {
        var beam = new ParticleStream { Kind = ParticleKind.Electron, Beta = 0.6 };
        string text = HudFormatter.Status(60, 0, 0, "Lab", Synced(0.6, beam), [beam]);
        Assert.Contains("Observer: moving at +0.6c along x (γ = 1.25)", text);
        Assert.Contains("#1 e⁻ β=0 γ=1", text); // riding with the beam
    }

    [Fact]
    public void Status_ShowsBothLinesOfAWire()
    {
        var wire = new ParticleStream { Kind = ParticleKind.NeutralWire, Beta = 0.5, Count = 10 };
        string text = HudFormatter.Status(60, 0, 0, "Lab", Synced(0, wire), [wire]);
        Assert.Contains("#1 wire  ions β=0  e⁻ β=+0.5", text);
        Assert.Contains("lab current", text);
    }

    [Theory]
    [InlineData(0.0, true, "Observer: lab frame")]
    [InlineData(-0.3, false, "Observer: moving at −0.3c along x (Galilean)")]
    public void ObserverDescription_DescribesFrame(double beta, bool relativistic, string expected) =>
        Assert.Equal(expected, HudFormatter.ObserverDescription(beta, relativistic));

    [Fact]
    public void ClockInfo_EmptyWithoutSelection() => Assert.Equal("", HudFormatter.ClockInfo(null, true));

    [Fact]
    public void ClockInfo_ClassicalMode()
    {
        var s = new ParticleStream();
        Assert.Contains("Classical", HudFormatter.ClockInfo(Synced(0, s).PrimaryLine(s), relativistic: false));
    }

    [Fact]
    public void ClockInfo_ShowsDilationFactor()
    {
        var s = new ParticleStream { Beta = 0.6 };
        string text = HudFormatter.ClockInfo(Synced(0, s).PrimaryLine(s), relativistic: true);
        Assert.Contains("γ = 1.25", text);
        Assert.Contains("0.8×", text);
    }

    [Fact]
    public void ClockInfo_ParticlesAtRestForObserver()
    {
        var s = new ParticleStream { Beta = 0.6 };
        Assert.Contains("at rest", HudFormatter.ClockInfo(Synced(0.6, s).PrimaryLine(s), relativistic: true));
    }
}
