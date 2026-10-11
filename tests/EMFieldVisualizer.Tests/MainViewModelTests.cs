using EMFieldVisualizer.Physics;

namespace EMFieldVisualizer.Tests;

public class MainViewModelTests
{
    [Fact]
    public void Constructor_LoadsFirstPresetAndSelectsIt()
    {
        var vm = new MainViewModel();
        Assert.Equal(0, vm.PresetIndex);
        var s = Assert.Single(vm.Streams);
        Assert.Equal(ParticleKind.Electron, s.Kind);
        Assert.Same(s, vm.SelectedStream);
        Assert.True(vm.HasSelection);
    }

    [Theory]
    [InlineData(0, 1, 0, 0)]
    [InlineData(1, 0, 0, 1)]
    [InlineData(2, 0, 0, 1)]
    [InlineData(3, 0, 0, 1)]
    [InlineData(4, 0, 0, 1)]
    [InlineData(5, 2, 0, 0)]
    [InlineData(6, 0, 2, 0)]
    [InlineData(7, 0, 2, 0)]
    [InlineData(8, 0, 1, 0)]
    [InlineData(9, 2, 2, 0)]
    public void Presets_CreateExpectedStreams(int preset, int electrons, int protons, int wires)
    {
        var vm = new MainViewModel { PresetIndex = preset };
        Assert.Equal(electrons, vm.Streams.Count(s => s.Kind == ParticleKind.Electron));
        Assert.Equal(protons, vm.Streams.Count(s => s.Kind == ParticleKind.Proton));
        Assert.Equal(wires, vm.Streams.Count(s => s.Kind == ParticleKind.NeutralWire));
        Assert.Equal(10, vm.Presets.Length);
    }

    [Fact]
    public void WirePreset_HasCurrentAndCoMovingNegativeProbe()
    {
        var vm = new MainViewModel { PresetIndex = 1 };
        var wire = vm.Streams.Single();
        Assert.True(wire.Beta > 0);
        Assert.Equal(-1, vm.Probe.Charge);
        Assert.Equal(wire.SignedBeta, vm.Probe.BetaX);
        Assert.Equal(0, vm.ObserverBeta);
    }

    [Fact]
    public void ElectronFramePreset_RidesWithTheDriftingElectrons()
    {
        var vm = new MainViewModel { PresetIndex = 2 };
        Assert.Equal(vm.Streams.Single().SignedBeta, vm.ObserverBeta);
        vm.PresetIndex = 0;
        Assert.Equal(0, vm.ObserverBeta); // other presets return to the lab
    }

    [Theory]
    [InlineData(3, 0.0, 0.5)] // no current, moving probe
    [InlineData(4, 0.5, 0.0)] // current, probe at rest
    public void NoForceWirePresets(int preset, double drift, double probeBeta)
    {
        var vm = new MainViewModel { PresetIndex = preset };
        Assert.Equal(drift, vm.Streams.Single().Beta);
        Assert.Equal(probeBeta, vm.Probe.BetaX);
    }

    [Fact]
    public void Observer_IsClampedAndDescribed()
    {
        var vm = new MainViewModel { ObserverBeta = 5 };
        Assert.Equal(MainViewModel.MaxObserverBeta, vm.ObserverBeta);
        Assert.Contains("moving at", vm.ObserverText);
        vm.ResetObserver();
        Assert.Equal(0, vm.ObserverBeta);
        Assert.Contains("lab frame", vm.ObserverText);
    }

    [Fact]
    public void RideWithSelectedStreamAndProbe()
    {
        var vm = new MainViewModel();
        vm.SelectedStream!.Beta = 0.4;
        vm.SelectedStream.Reversed = true;
        vm.RideWithSelectedStream();
        Assert.Equal(-0.4, vm.ObserverBeta);

        vm.Probe.BetaX = 0.25;
        vm.RideWithProbe();
        Assert.Equal(0.25, vm.ObserverBeta);
    }

    [Fact]
    public void AddStream_WireGetsCurrentByDefault()
    {
        var vm = new MainViewModel();
        vm.AddStream(ParticleKind.NeutralWire);
        Assert.True(vm.SelectedStream!.IsWire);
        Assert.True(vm.SelectedStream.WireCurrent > 0);
    }

    [Fact]
    public void PancakePreset_SwitchesToPlaneSliceAndOthersSwitchBack()
    {
        var vm = new MainViewModel { PresetIndex = 8 };
        Assert.Equal(SliceMode.XYThroughProbe, vm.Slice);
        vm.PresetIndex = 0;
        Assert.Equal(SliceMode.Volume, vm.Slice);
    }

    [Fact]
    public void AddStream_PlacesStreamsAtDistinctPositionsAndSelectsNewOne()
    {
        var vm = new MainViewModel();
        vm.Streams.Clear();
        for (int i = 0; i < 4; i++) vm.AddStream(ParticleKind.Proton);

        Assert.Equal(4, vm.Streams.Count);
        Assert.Equal(4, vm.Streams.Select(s => (s.OffsetY, s.OffsetZ)).Distinct().Count());
        Assert.Same(vm.Streams[^1], vm.SelectedStream);
        Assert.Equal(ParticleKind.Proton, vm.SelectedStream!.Kind);
    }

    [Fact]
    public void RemoveSelected_SelectsNeighbourThenNothing()
    {
        var vm = new MainViewModel { PresetIndex = 5 }; // two streams
        var second = vm.Streams[1];
        vm.SelectedStream = vm.Streams[0];

        vm.RemoveSelected();
        Assert.Same(second, vm.SelectedStream);

        vm.RemoveSelected();
        Assert.Empty(vm.Streams);
        Assert.Null(vm.SelectedStream);
        Assert.False(vm.HasSelection);

        vm.RemoveSelected(); // no-op
    }

    [Fact]
    public void MatchProbeToSelectedStream_CopiesSignedVelocity()
    {
        var vm = new MainViewModel();
        vm.SelectedStream!.Beta = 0.7;
        vm.SelectedStream.Reversed = true;
        vm.Probe.BetaY = 0.3;

        vm.MatchProbeToSelectedStream();

        Assert.Equal(-0.7, vm.Probe.BetaX);
        Assert.Equal(0, vm.Probe.BetaY);
        Assert.Equal(0, vm.Probe.BetaZ);
    }

    [Fact]
    public void ViewMode_UpdatesFlagsAndHelpText()
    {
        var vm = new MainViewModel();
        Assert.True(vm.IsLabFrame);
        string labHelp = vm.ViewHelpText;

        vm.ViewModeIndex = (int)FieldView.PureMagnetic;

        Assert.False(vm.IsLabFrame);
        Assert.Equal(FieldView.PureMagnetic, vm.ViewMode);
        Assert.NotEqual(labHelp, vm.ViewHelpText);
        Assert.Contains("purely magnetic", vm.ViewHelpText);
    }

    [Fact]
    public void ForceArrows_ShowStrengthByDefault() => Assert.True(new MainViewModel().AbsoluteForceArrows);

    [Fact]
    public void ClassicalMode_IsMentionedInHelpText()
    {
        var vm = new MainViewModel { Relativistic = false };
        Assert.Contains("Classical", vm.ViewHelpText);
    }

    [Fact]
    public void OptionLists_MatchEnums()
    {
        var vm = new MainViewModel();
        Assert.Equal(Enum.GetValues<FieldView>().Length, vm.ViewModes.Length);
        Assert.Equal(Enum.GetValues<SliceMode>().Length, vm.SliceModes.Length);
        Assert.Equal(Enum.GetValues<ParticleKind>(), vm.KindOptions.Select(k => k.Kind));
        Assert.All(vm.KindOptions, k => Assert.False(string.IsNullOrWhiteSpace(k.Name)));
    }
}
