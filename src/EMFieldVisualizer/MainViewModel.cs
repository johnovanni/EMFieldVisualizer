using System.Collections.ObjectModel;
using EMFieldVisualizer.Physics;

namespace EMFieldVisualizer;

/// <summary>A stream type with a readable name for the type picker.</summary>
public sealed record KindOption(ParticleKind Kind, string Name);

public sealed class MainViewModel : ObservableObject
{
    private ParticleStream? _selectedStream;
    private bool _showElectric = true;
    private bool _showMagnetic = true;
    private int _viewMode;
    private int _sliceMode;
    private bool _relativistic = true;
    private bool _contractSpacing;
    private bool _periodic = true;
    private int _gridResolution = 6;
    private double _arrowScale = 1.0;
    private bool _commonScale;
    private double _animationSpeed = 12;
    private bool _paused;
    private bool _showParticles = true;
    private bool _showProbeForces = true;
    private bool _absoluteForceArrows = true;
    private double _observerBeta;
    private int _presetIndex = -1;
    private string _probeText = "";
    private string _hudText = "";
    private string _viewHelpText = "";

    public MainViewModel()
    {
        PresetIndex = 0;
        UpdateViewHelp();
    }

    public ObservableCollection<ParticleStream> Streams { get; } = [];
    public Probe Probe { get; } = new();

    /// <summary>Largest observer speed the slider allows (c).</summary>
    public const double MaxObserverBeta = 0.99;

    public KindOption[] KindOptions { get; } =
    [
        new(ParticleKind.Electron, "Electron beam"),
        new(ParticleKind.Proton, "Proton beam"),
        new(ParticleKind.NeutralWire, "Neutral wire (ions + drifting electrons)"),
    ];

    public string[] Presets { get; } =
    [
        "Single electron beam",
        "Neutral wire with current: probe moving with the electrons",
        "Same wire, seen by an observer riding with the electrons",
        "Neutral wire, NO current: moving probe (no force)",
        "Neutral wire with current: probe at rest (no force)",
        "Two parallel electron beams",
        "Two parallel proton beams",
        "Opposing proton beams (anti-parallel)",
        "Single ultra-relativistic proton (field pancake)",
        "Mixed: 2 electron + 2 proton beams",
    ];

    public string[] ViewModes { get; } =
    [
        "Lab frame: E and B as measured",
        "Equivalent purely ELECTRIC field (E′, local frame where B′ = 0)",
        "Equivalent purely MAGNETIC field (B′, local frame where E′ = 0)",
    ];

    public string[] SliceModes { get; } =
    [
        "Full 3D volume",
        "Cross-section (YZ plane) through probe",
        "XY plane through probe",
        "XZ plane through probe",
    ];

    public ParticleStream? SelectedStream
    {
        get => _selectedStream;
        set
        {
            if (Set(ref _selectedStream, value)) OnPropertyChanged(nameof(HasSelection));
        }
    }

    public bool HasSelection => SelectedStream != null;

    public bool ShowElectric { get => _showElectric; set => Set(ref _showElectric, value); }
    public bool ShowMagnetic { get => _showMagnetic; set => Set(ref _showMagnetic, value); }

    public int ViewModeIndex
    {
        get => _viewMode;
        set
        {
            if (!Set(ref _viewMode, value)) return;
            OnPropertyChanged(nameof(IsLabFrame));
            UpdateViewHelp();
        }
    }

    public FieldView ViewMode => (FieldView)ViewModeIndex;
    public bool IsLabFrame => ViewMode == FieldView.LabFrame;

    public int SliceModeIndex { get => _sliceMode; set => Set(ref _sliceMode, value); }
    public SliceMode Slice => (SliceMode)SliceModeIndex;

    public bool Relativistic
    {
        get => _relativistic;
        set { if (Set(ref _relativistic, value)) UpdateViewHelp(); }
    }

    public bool ContractSpacing { get => _contractSpacing; set => Set(ref _contractSpacing, value); }
    public bool Periodic { get => _periodic; set => Set(ref _periodic, value); }
    public int GridResolution { get => _gridResolution; set => Set(ref _gridResolution, value); }
    public double ArrowScale { get => _arrowScale; set => Set(ref _arrowScale, value); }
    public bool CommonScale { get => _commonScale; set => Set(ref _commonScale, value); }
    public double AnimationSpeed { get => _animationSpeed; set => Set(ref _animationSpeed, value); }
    public bool Paused { get => _paused; set => Set(ref _paused, value); }
    public bool ShowParticles { get => _showParticles; set => Set(ref _showParticles, value); }
    public bool ShowProbeForces { get => _showProbeForces; set => Set(ref _showProbeForces, value); }

    /// <summary>True: force arrows show strength (√-compressed, fixed reference). False: direction/relative size only.</summary>
    public bool AbsoluteForceArrows { get => _absoluteForceArrows; set => Set(ref _absoluteForceArrows, value); }

    /// <summary>Velocity along x of the frame the scene is viewed from, relative to the lab (c).</summary>
    public double ObserverBeta
    {
        get => _observerBeta;
        set
        {
            if (Set(ref _observerBeta, Math.Clamp(value, -MaxObserverBeta, MaxObserverBeta)))
                OnPropertyChanged(nameof(ObserverText));
        }
    }

    public string ObserverText => Math.Abs(ObserverBeta) < 1e-9
        ? "You are watching from the lab frame (the frame where wires are at rest)."
        : $"You are moving at {ObserverBeta:+0.###;−0.###}c along x relative to the lab (γ = {Phys.Gamma(ObserverBeta):0.###}). " +
          "Particle speeds, spacing, clocks and fields are all shown as you would measure them; the probe keeps its lab velocity, so it moves relative to you.";

    /// <summary>Watch from the frame of the selected stream's moving particles (a wire's electrons).</summary>
    public void RideWithSelectedStream()
    {
        if (SelectedStream != null) ObserverBeta = SelectedStream.SignedBeta;
    }

    /// <summary>Watch from the probe's rest frame (its x velocity).</summary>
    public void RideWithProbe() => ObserverBeta = Probe.BetaX;

    public void ResetObserver() => ObserverBeta = 0;

    public string ProbeText { get => _probeText; set => Set(ref _probeText, value); }
    public string HudText { get => _hudText; set => Set(ref _hudText, value); }
    public string ViewHelpText { get => _viewHelpText; private set => Set(ref _viewHelpText, value); }

    public int PresetIndex
    {
        get => _presetIndex;
        set
        {
            if (Set(ref _presetIndex, value) && value >= 0) ApplyPreset(value);
        }
    }

    public void AddStream(ParticleKind kind)
    {
        double y = 0, z = 0;
        // Place the new stream on a free spot around the axis.
        for (int ring = 0; ring < 6; ring++)
        {
            y = ring == 0 ? 0 : 6 * Math.Cos(ring * Math.PI / 3);
            z = ring == 0 ? 0 : 6 * Math.Sin(ring * Math.PI / 3);
            double cy = y, cz = z;
            if (!Streams.Any(s => Math.Abs(s.OffsetY - cy) < 0.5 && Math.Abs(s.OffsetZ - cz) < 0.5)) break;
        }
        bool wire = kind == ParticleKind.NeutralWire;
        var stream = new ParticleStream
        {
            Kind = kind, OffsetY = Math.Round(y, 1), OffsetZ = Math.Round(z, 1),
            Beta = wire ? 0.5 : 0.6, Count = wire ? 30 : 20,
        };
        Streams.Add(stream);
        SelectedStream = stream;
    }

    /// <summary>Gives the probe the selected stream's velocity (so it rides along with the stream).</summary>
    public void MatchProbeToSelectedStream()
    {
        if (SelectedStream == null) return;
        Probe.BetaX = SelectedStream.Direction * SelectedStream.Beta;
        Probe.BetaY = 0;
        Probe.BetaZ = 0;
    }

    public void RemoveSelected()
    {
        if (SelectedStream == null) return;
        int idx = Streams.IndexOf(SelectedStream);
        Streams.Remove(SelectedStream);
        SelectedStream = Streams.Count == 0 ? null : Streams[Math.Min(idx, Streams.Count - 1)];
    }

    private void ApplyPreset(int index)
    {
        Streams.Clear();
        ParticleStream S(ParticleKind k, double y, double z, double beta, int n, bool reversed = false)
        {
            var s = new ParticleStream { Kind = k, OffsetY = y, OffsetZ = z, Beta = beta, Count = n, Reversed = reversed };
            Streams.Add(s);
            return s;
        }

        var e = ParticleKind.Electron;
        var p = ParticleKind.Proton;
        var w = ParticleKind.NeutralWire;
        Probe.Position = new(0, 6, 0);
        Probe.BetaX = 0; Probe.BetaY = 0; Probe.BetaZ = 0;
        Probe.Charge = 1;
        ContractSpacing = false;
        ObserverBeta = 0;

        switch (index)
        {
            case 0:
                S(e, 0, 0, 0.6, 24);
                break;
            case 1: // current-carrying wire; electron probe riding with the drifting electrons → magnetic attraction
                S(w, 0, 0, 0.5, 40);
                Probe.Charge = -1;
                Probe.BetaX = 0.5;
                break;
            case 2: // the same, from the electrons' frame → the wire is charged and the force is electric
                S(w, 0, 0, 0.5, 40);
                Probe.Charge = -1;
                Probe.BetaX = 0.5;
                ObserverBeta = 0.5;
                break;
            case 3: // no current: moving past a neutral wire does nothing
                S(w, 0, 0, 0.0, 40);
                Probe.Charge = -1;
                Probe.BetaX = 0.5;
                break;
            case 4: // current, but the probe is at rest: B exists but v×B = 0, and E = 0
                S(w, 0, 0, 0.5, 40);
                Probe.Charge = -1;
                break;
            case 5:
                S(e, -5, 0, 0.8, 20);
                S(e, 5, 0, 0.8, 20);
                Probe.Position = new(0, 9, 0);
                Probe.Charge = -1;
                Probe.BetaX = 0.8;
                break;
            case 6:
                S(p, -5, 0, 0.8, 20);
                S(p, 5, 0, 0.8, 20);
                Probe.Position = new(0, 9, 0);
                Probe.BetaX = 0.8;
                break;
            case 7:
                S(p, -5, 0, 0.7, 20);
                S(p, 5, 0, 0.7, 20, reversed: true);
                Probe.Position = new(0, 9, 0);
                Probe.BetaX = 0.7;
                break;
            case 8:
                S(p, 0, 0, 0.95, 1);
                Probe.Position = new(0, 5, 0);
                SliceModeIndex = (int)SliceMode.XYThroughProbe;
                break;
            case 9:
                S(e, -6, -6, 0.9, 20);
                S(e, 6, 6, 0.5, 20);
                S(p, -6, 6, 0.3, 20);
                S(p, 6, -6, 0.7, 20, reversed: true);
                Probe.Position = new(0, 0, 0);
                break;
        }
        if (index != 8 && Slice == SliceMode.XYThroughProbe) SliceModeIndex = (int)SliceMode.Volume;
        SelectedStream = Streams.FirstOrDefault();
    }

    private void UpdateViewHelp()
    {
        ViewHelpText = ViewMode switch
        {
            FieldView.PureElectric =>
                "At every point where E > cB there is a frame (moving along E×B) in which the magnetic field vanishes. " +
                "Arrows show that purely electric field E′ = √(E² − c²B²). Points where cB ≥ E have no such frame and are hidden.",
            FieldView.PureMagnetic =>
                "At every point where cB > E there is a frame (moving along E×B) in which the electric field vanishes. " +
                "Arrows show that purely magnetic field B′ = √(B² − E²/c²). Points where E ≥ cB have no such frame and are hidden.",
            _ => "Orange/red arrows: electric field E.  Cyan/violet arrows: magnetic field B. " +
                 "Arrow length is compressed (√) and colour shows strength; a field under ~5 % of one stream's own field is drawn tiny.",
        };
        if (!Relativistic) ViewHelpText += "  (Classical mode: no field flattening, contraction or dilation.)";
    }
}
