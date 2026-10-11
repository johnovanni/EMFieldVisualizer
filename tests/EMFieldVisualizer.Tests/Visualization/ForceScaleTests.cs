using EMFieldVisualizer.Physics;
using EMFieldVisualizer.Visualization;
using static EMFieldVisualizer.Tests.TestHelpers;

namespace EMFieldVisualizer.Tests.Visualization;

public class ForceScaleTests : InvariantCultureTest
{
    private const double Max = 6;

    // ---------- normalized ----------

    [Fact]
    public void Normalized_LargestIsFullLengthOthersProportional()
    {
        var s = ForceScale.Normalized;
        AssertClose(Max, s.Length(4, 4, Max));
        AssertClose(Max / 2, s.Length(2, 4, Max));
        Assert.Equal(0, s.Length(0, 4, Max));
        Assert.Equal(0, s.Length(1, 0, Max));
    }

    [Fact]
    public void Normalized_IgnoresAbsoluteMagnitude() =>
        AssertClose(ForceScale.Normalized.Length(1e-20, 1e-20, Max), ForceScale.Normalized.Length(1e5, 1e5, Max));

    [Fact]
    public void Normalized_UsesMidToneColour() => Assert.Equal(0.5, ForceScale.Normalized.ColorCoordinate(123));

    // ---------- absolute ----------

    [Fact]
    public void Absolute_ReferenceForceIsHalfLength() =>
        AssertClose(Max * ForceScale.ReferenceFraction, ForceScale.AbsoluteTo(2e-12).Length(2e-12, 99, Max));

    [Fact]
    public void Absolute_LengthIsSqrtCompressedAndIndependentOfLargestInSet()
    {
        var s = ForceScale.AbsoluteTo(1);
        AssertClose(2 * s.Length(0.25, 1, Max), s.Length(1, 1, Max));
        AssertClose(s.Length(0.25, 0.25, Max), s.Length(0.25, 1000, Max));
    }

    [Fact]
    public void Absolute_PreservesRatiosBetweenArrowsInTheSameSet()
    {
        var s = ForceScale.AbsoluteTo(1);
        // Electric 1, magnetic 0.25: the magnetic arrow is √0.25 = half as long, whatever the total is.
        AssertClose(0.5, s.Length(0.25, 1, Max) / s.Length(1, 1, Max));
    }

    [Fact]
    public void Absolute_IsClampedToRange()
    {
        var s = ForceScale.AbsoluteTo(1);
        Assert.Equal(Max, s.Length(1e6, 1e6, Max));
        AssertClose(Max * ForceScale.MinFraction, s.Length(ForceScale.HideBelowRatio * 2, 1, Max));
    }

    [Fact]
    public void Absolute_HidesNegligibleForces()
    {
        var s = ForceScale.AbsoluteTo(1);
        Assert.Equal(0, s.Length(ForceScale.HideBelowRatio / 2, 1, Max));
        Assert.Equal(0, s.Length(0, 1, Max));
    }

    [Fact]
    public void Absolute_ColourBrightensWithStrength()
    {
        var s = ForceScale.AbsoluteTo(1);
        Assert.True(s.ColorCoordinate(10) > s.ColorCoordinate(1));
        Assert.True(s.ColorCoordinate(1) > s.ColorCoordinate(0.01));
    }

    [Fact]
    public void AbsoluteWithoutReference_FallsBackToNormalized()
    {
        var s = ForceScale.AbsoluteTo(0);
        AssertClose(Max, s.Length(3, 3, Max));
        Assert.Contains("normalized", s.Describe());
    }

    [Fact]
    public void Describe_NamesMode()
    {
        Assert.Contains("normalized", ForceScale.Normalized.Describe());
        Assert.Contains("√-scaled", ForceScale.AbsoluteTo(1e-12).Describe());
        Assert.Contains("1 pN", ForceScale.AbsoluteTo(1e-12).Describe());
    }

    // ---------- the probe moving away gets shorter arrows ----------

    [Fact]
    public void MovingProbeAway_ShrinksArrowInAbsoluteModeOnly()
    {
        var line = new ParticleStream { Kind = ParticleKind.Proton, Beta = 0.5, Count = 40 };
        var sim = SimulationWith(periodic: true, relativistic: true, line);
        var absolute = ForceScale.AbsoluteTo(ForceReference.Compute(sim));

        double LengthAt(double y, ForceScale scale)
        {
            var f = LorentzForce.Compute(sim, new Probe { Position = new Point3D(0, y, 0) });
            return scale.Length(f.Total.Length, f.MaxMagnitude, Max);
        }

        Assert.True(LengthAt(3, absolute) > LengthAt(6, absolute));
        Assert.True(LengthAt(6, absolute) > LengthAt(15, absolute));
        AssertClose(LengthAt(3, ForceScale.Normalized), LengthAt(15, ForceScale.Normalized));
    }
}

public class ForceReferenceTests
{
    private static double Ref(Simulation sim, params ParticleStream[] streams)
    {
        sim.Sync(streams);
        return ForceReference.Compute(sim);
    }

    [Fact]
    public void Wire_CountsItsDensestLineNotItsNetCharge()
    {
        var sim = new Simulation();
        AssertClose(Ref(sim, new ParticleStream { Count = 30 }), Ref(sim, new ParticleStream { Kind = ParticleKind.NeutralWire, Count = 30 }), 1e-12);
    }

    [Fact]
    public void NoEnabledStreams_GivesZero()
    {
        var sim = new Simulation();
        Assert.Equal(0, Ref(sim));
        Assert.Equal(0, Ref(sim, new ParticleStream { Enabled = false }));
    }

    [Fact]
    public void MatchesLineChargeForceAtReferenceDistance()
    {
        var sim = new Simulation();
        double f = Ref(sim, new ParticleStream { Count = 40 });
        // λ = 40 e / 40 nm = 1 e/nm  →  F = e · 2kλ / 5 nm
        double expected = Phys.ElementaryCharge * 2 * Phys.K * (Phys.ElementaryCharge / 1e-9) / 5e-9;
        AssertClose(expected, f, 1e-12);
    }

    [Fact]
    public void UsesDensestStreamAndIsIndependentOfKindSpeedAndPosition()
    {
        var sim = new Simulation();
        double baseline = Ref(sim, new ParticleStream { Count = 30 });
        double mixed = Ref(sim,
        
            new ParticleStream { Count = 10 },
            new ParticleStream { Count = 30, Kind = ParticleKind.Electron, Beta = 0.9, OffsetY = 8 });
        AssertClose(baseline, mixed, 1e-12);
    }

    [Fact]
    public void FollowsContractedSpacing()
    {
        var stream = new ParticleStream { Count = 20, Beta = 0.8 };
        double plain = Ref(new Simulation { ContractSpacing = false }, stream);
        double contracted = Ref(new Simulation { ContractSpacing = true }, stream);
        AssertClose(20 * (5.0 / 3.0) / 20, contracted / plain, 1e-12); // exact density, not the rounded 33
    }

    [Fact]
    public void ReferenceMatchesSimulatedFieldOfDenseLine()
    {
        // The analytic reference should agree with the simulated force at the reference distance.
        var line = new ParticleStream { Kind = ParticleKind.Proton, Count = 200 };
        var sim = SimulationWith(periodic: true, relativistic: true, line);
        var f = LorentzForce.Compute(sim, new Probe { Position = new Point3D(0, ForceReference.Distance, 0) });
        AssertClose(ForceReference.Compute(sim), f.Total.Length, relativeTolerance: 0.01);
    }
}
