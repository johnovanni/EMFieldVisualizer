using System.ComponentModel;
using EMFieldVisualizer.Physics;
using static EMFieldVisualizer.Tests.TestHelpers;

namespace EMFieldVisualizer.Tests.Physics;

public class ParticleStreamTests : InvariantCultureTest
{
    [Theory]
    [InlineData(-0.5, 0.0)]
    [InlineData(0.5, 0.5)]
    [InlineData(1.0, 0.9999)]
    [InlineData(2.0, 0.9999)]
    public void Beta_IsClampedBelowLightSpeed(double input, double expected)
    {
        var s = new ParticleStream { Beta = input };
        Assert.Equal(expected, s.Beta);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(50, 50)]
    [InlineData(1000, 200)]
    public void Count_IsClamped(int input, int expected) =>
        Assert.Equal(expected, new ParticleStream { Count = input }.Count);

    [Fact]
    public void ChargeSign_FollowsKind()
    {
        Assert.Equal(-1, new ParticleStream { Kind = ParticleKind.Electron }.ChargeSign);
        Assert.Equal(1, new ParticleStream { Kind = ParticleKind.Proton }.ChargeSign);
    }

    [Fact]
    public void Direction_IsNegativeWhenReversed()
    {
        Assert.Equal(1, new ParticleStream().Direction);
        Assert.Equal(-1, new ParticleStream { Reversed = true }.Direction);
    }

    [Fact]
    public void RestEnergy_FollowsKind()
    {
        Assert.Equal(Phys.ElectronRestMeV, new ParticleStream { Kind = ParticleKind.Electron }.RestEnergyMeV);
        Assert.Equal(Phys.ProtonRestMeV, new ParticleStream { Kind = ParticleKind.Proton }.RestEnergyMeV);
    }

    [Fact]
    public void ChangingBeta_RaisesDerivedPropertyNotifications()
    {
        var s = new ParticleStream { Beta = 0.1 };
        var changed = new List<string?>();
        ((INotifyPropertyChanged)s).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        s.Beta = 0.6;

        Assert.Contains(nameof(ParticleStream.Beta), changed);
        Assert.Contains(nameof(ParticleStream.Gamma), changed);
        Assert.Contains(nameof(ParticleStream.Summary), changed);
        Assert.Contains(nameof(ParticleStream.RelativityText), changed);
    }

    [Fact]
    public void SettingSameValue_DoesNotNotify()
    {
        var s = new ParticleStream { Beta = 0.6 };
        int count = 0;
        ((INotifyPropertyChanged)s).PropertyChanged += (_, _) => count++;
        s.Beta = 0.6;
        Assert.Equal(0, count);
    }

    [Fact]
    public void RelativityText_ShowsGammaContractionAndKineticEnergy()
    {
        var s = new ParticleStream { Kind = ParticleKind.Electron, Beta = 0.6 };
        Assert.Contains("γ = 1.25", s.RelativityText);
        Assert.Contains("×0.8", s.RelativityText);
        // KE = (γ − 1) mc² = 0.25 × 511 keV ≈ 128 keV
        Assert.Contains("128 keV", s.RelativityText);
    }

    [Fact]
    public void Wire_IsNeutralAndCarriesCurrentProportionalToDrift()
    {
        var w = new ParticleStream { Kind = ParticleKind.NeutralWire, Count = 40, Beta = 0.5 };
        Assert.True(w.IsWire);
        Assert.Equal(0, w.ChargeSign);
        // I = (40 e / 40 nm) × 0.5c ≈ 24 mA
        AssertClose(Phys.ElementaryCharge * 1e9 * 0.5 * Phys.C, w.WireCurrent, 1e-12);
        Assert.Contains("Current I = 24 mA", w.RelativityText);
        Assert.Contains("drift", w.SpeedLabel);
        Assert.StartsWith("Neutral wire ×40", w.Summary);
    }

    [Fact]
    public void Wire_WithoutDriftHasNoCurrent()
    {
        var w = new ParticleStream { Kind = ParticleKind.NeutralWire, Beta = 0 };
        Assert.Equal(0, w.WireCurrent);
        Assert.Contains("No current", w.RelativityText);
    }

    [Fact]
    public void Beams_CarryNoWireCurrent() => Assert.Equal(0, new ParticleStream { Beta = 0.5 }.WireCurrent);

    [Fact]
    public void SignedBeta_FollowsDirection() =>
        Assert.Equal(-0.3, new ParticleStream { Beta = 0.3, Reversed = true }.SignedBeta);

    [Fact]
    public void ChangingKind_NotifiesLabels()
    {
        var s = new ParticleStream();
        var changed = new List<string?>();
        ((INotifyPropertyChanged)s).PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        s.Kind = ParticleKind.NeutralWire;
        Assert.Contains(nameof(ParticleStream.SpeedLabel), changed);
        Assert.Contains(nameof(ParticleStream.IsWire), changed);
    }

    [Fact]
    public void Summary_MarksDisabledAndReversedStreams()
    {
        var s = new ParticleStream { Reversed = true, Enabled = false };
        Assert.Contains("(−x)", s.Summary);
        Assert.Contains("[off]", s.Summary);
    }
}
