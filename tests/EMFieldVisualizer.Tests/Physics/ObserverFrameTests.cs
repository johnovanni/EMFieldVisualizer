using EMFieldVisualizer.Physics;
using EMFieldVisualizer.Visualization;
using static EMFieldVisualizer.Tests.TestHelpers;

namespace EMFieldVisualizer.Tests.Physics;

/// <summary>The same physics seen by different observers, and the neutral wire.</summary>
public class ObserverFrameTests
{
    private const double Q = Phys.ElementaryCharge;

    private static Simulation Sim(double observerBeta, bool relativistic, params ParticleStream[] streams)
    {
        var sim = new Simulation { ObserverBeta = observerBeta, Relativistic = relativistic, Periodic = true };
        sim.Prepare(streams);
        return sim;
    }

    private static ParticleStream Wire(double drift, int count = 200) =>
        new() { Kind = ParticleKind.NeutralWire, Beta = drift, Count = count };

    private static Probe ElectronProbe(double betaX) =>
        new() { Position = new Point3D(0, 6, 0), Charge = -1, BetaX = betaX };

    // ---------- lines as seen by the observer ----------

    [Fact]
    public void BeamVelocity_IsTransformed()
    {
        var beam = new ParticleStream { Beta = 0.6 };
        Assert.Equal(0, Sim(0.6, true, beam).PrimaryLine(beam)!.Beta, 12);
        AssertClose(1.2 / 1.36, Sim(-0.6, true, beam).PrimaryLine(beam)!.Beta, 1e-12);
    }

    [Fact]
    public void Wire_IsTwoLinesThatAreNeutralInTheLab()
    {
        var wire = Wire(0.5, count: 40);
        var lines = Sim(0, true, wire).LinesFor(wire).ToList();
        Assert.Equal(2, lines.Count);
        var ions = lines.Single(l => l.Role == LineRole.WireIons);
        var electrons = lines.Single(l => l.Role == LineRole.WireElectrons);
        Assert.Equal(ParticleKind.Proton, ions.Species);
        Assert.Equal(ParticleKind.Electron, electrons.Species);
        Assert.Equal(0, ions.Beta);
        Assert.Equal(0.5, electrons.Beta);
        Assert.Equal(ions.Count, electrons.Count);
    }

    [Fact]
    public void Wire_InElectronFrame_IonsContractAndElectronsSpreadOut()
    {
        var wire = Wire(0.5, count: 40);  // γ = 1.1547
        var sim = Sim(0.5, true, wire);
        var ions = sim.LinesFor(wire).Single(l => l.Role == LineRole.WireIons);
        var electrons = sim.LinesFor(wire).Single(l => l.Role == LineRole.WireElectrons);

        AssertClose(-0.5, ions.Beta, 1e-12);
        Assert.Equal(46, ions.Count);       // 40 × γ = 46.2
        Assert.Equal(0, electrons.Beta, 12);
        Assert.Equal(35, electrons.Count);  // 40 / γ = 34.6
    }

    [Fact]
    public void Wire_InGalileanElectronFrame_StaysNeutral()
    {
        var wire = Wire(0.5, count: 40);
        var sim = Sim(0.5, false, wire);
        Assert.All(sim.LinesFor(wire), l => Assert.Equal(40, l.Count));
    }

    [Fact]
    public void ProbeVelocity_IsTransformed()
    {
        var sim = Sim(0.5, true, Wire(0.5));
        AssertClose(new Vector3D(), sim.ToObserverVelocity(new Vector3D(0.5, 0, 0)), 1e-12);
    }

    // ---------- fields transform correctly ----------

    [Fact]
    public void SingleChargeField_InObserverFrame_EqualsLorentzTransformedLabField()
    {
        // Point directly beside the charge (Δx = 0), so the lab and observer snapshots are the same event.
        var proton = SingleParticle(ParticleKind.Proton, 0.6);
        var point = new Point3D(-Simulation.Length / 2, 10, 0);

        var lab = new Simulation { Periodic = false };
        lab.Prepare([proton]);
        lab.FieldAt(point, out var eLab, out var bLab);

        var moving = new Simulation { Periodic = false, ObserverBeta = 0.3 };
        moving.Prepare([proton]);
        moving.FieldAt(point, out var eObs, out var bObs);

        var (eExpected, bExpected) = Lorentz.Fields(eLab, bLab, 0.3);
        AssertClose(eExpected, eObs, eExpected.Length * 1e-9);
        AssertClose(bExpected, bObs, bExpected.Length * 1e-9);
    }

    [Fact]
    public void RidingWithABeam_ItsMagneticFieldVanishes()
    {
        var beam = new ParticleStream { Kind = ParticleKind.Electron, Beta = 0.6, Count = 100 };
        Sim(0.6, true, beam).FieldAt(new Point3D(0, 5, 0), out var e, out var b);
        Assert.True(e.Length > 0);
        Assert.Equal(0, b.Length);
    }

    // ---------- the neutral wire ----------

    [Fact]
    public void WireWithoutCurrent_ExertsNoForceEvenOnAMovingCharge()
    {
        var sim = Sim(0, true, Wire(0));
        var f = LorentzForce.Compute(sim, ElectronProbe(0.5));
        Assert.True(f.Total.Length < 1e-9 * ForceReference.Compute(sim)); // exact cancellation up to rounding
    }

    [Fact]
    public void WireWithCurrent_HasMagneticFieldButNoForceOnAChargeAtRest()
    {
        var sim = Sim(0, true, Wire(0.5));
        var f = LorentzForce.Compute(sim, ElectronProbe(0));
        Assert.True(f.B.Length > 0);
        Assert.Equal(0, f.Magnetic.Length);
        Assert.True(f.Total.Length < 1e-3 * ForceReference.Compute(sim)); // E ≈ 0: neutral
    }

    [Fact]
    public void WireWithCurrent_AttractsChargeMovingWithElectrons_Magnetically()
    {
        const double drift = 0.5, r = 6;
        const int n = 200;
        var f = LorentzForce.Compute(Sim(0, true, Wire(drift, n)), ElectronProbe(drift));

        // F = q v B with B = μ0 I / 2πr  →  e · β² · 2kλ / r
        double lambda = n * Q / (Simulation.Length * Phys.MetresPerUnit);
        double expected = Q * drift * drift * 2 * Phys.K * lambda / (r * Phys.MetresPerUnit);
        AssertClose(expected, f.Magnetic.Length, relativeTolerance: 0.01);
        Assert.True(f.Total.Y < 0); // towards the wire
        Assert.True(f.Electric.Length < 1e-3 * f.Magnetic.Length);
    }

    [Fact]
    public void WireWithCurrent_InElectronFrame_ForceIsPurelyElectricAndAgreesWithLab()
    {
        const double drift = 0.5;
        var wire = Wire(drift);
        var probe = ElectronProbe(drift);

        var lab = LorentzForce.Compute(Sim(0, true, wire), probe);
        var obs = LorentzForce.Compute(Sim(drift, true, wire), probe);

        Assert.Equal(0, obs.Magnetic.Length, 12); // probe at rest in this frame
        Assert.True(obs.Electric.Y < 0);           // the wire looks positively charged: attraction

        var check = FrameCheck.Compute(obs, lab, drift, relativistic: true);
        Assert.True(check.RelativeDifference < 0.005, $"differ by {check.RelativeDifference:P2}");
    }

    [Theory]
    [InlineData(40)]  // the preset: rounding to whole particles (46 ions, 35 electrons) is compensated by charge scaling
    [InlineData(200)]
    public void WireCrossCheck_AgreesForPresetSizedWires(int count)
    {
        var wire = Wire(0.5, count);
        var probe = ElectronProbe(0.5);
        var lab = LorentzForce.Compute(Sim(0, true, wire), probe);
        var obs = LorentzForce.Compute(Sim(0.5, true, wire), probe);
        Assert.True(FrameCheck.Compute(obs, lab, 0.5, true).RelativeDifference < 0.01);
    }

    [Fact]
    public void WireWithCurrent_LabElectricFieldIsNegligibleEverywhere()
    {
        var sim = Sim(0, true, Wire(0.5, 40));
        double lineField = ForceReference.Compute(sim) / Q;
        foreach (var p in new[] { new Point3D(0, 3, 0), new Point3D(7, 0, -9), new Point3D(-15, 12, 4) })
        {
            sim.FieldAt(p, out var e, out _);
            Assert.True(e.Length < 0.01 * lineField, $"E = {e.Length:E2} at {p}");
        }
    }

    [Fact]
    public void WireWithCurrent_GalileanElectronFrame_DisagreesWithLab()
    {
        const double drift = 0.5;
        var wire = Wire(drift);
        var probe = ElectronProbe(drift);

        var lab = LorentzForce.Compute(Sim(0, false, wire), probe);
        var obs = LorentzForce.Compute(Sim(drift, false, wire), probe);

        var check = FrameCheck.Compute(obs, lab, drift, relativistic: false);
        Assert.True(check.RelativeDifference > 0.9); // lab: magnetic pull; Galilean electron frame: nothing
    }
}

public class FrameCheckTests
{
    [Fact]
    public void Compute_ConvertsObserverForceAndKeepsOnlyTransverseLabForce()
    {
        var obs = new ProbeForces(default, default, new Vector3D(1, 2.5, 0), default, Beta: new Vector3D());
        var lab = new ProbeForces(default, default, new Vector3D(7, 2, 0), default);
        var check = FrameCheck.Compute(obs, lab, 0.6, relativistic: true);

        AssertClose(new Vector3D(0, 2, 0), check.FromObserver, 1e-12);
        AssertClose(new Vector3D(0, 2, 0), check.InLab, 1e-12);
        AssertClose(0, check.RelativeDifference, absoluteTolerance: 1e-12);
    }

    [Fact]
    public void RelativeDifference_IsZeroWhenBothVanish() =>
        Assert.Equal(0, new FrameCheck(0.5, true, default, default, default).RelativeDifference);

    [Fact]
    public void RelativeDifference_IsOneWhenOnlyOneSideHasForce() =>
        AssertClose(1, new FrameCheck(0.5, false, default, new Vector3D(0, 1, 0), default).RelativeDifference);
}
