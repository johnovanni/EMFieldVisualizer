using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using EMFieldVisualizer.Input;
using EMFieldVisualizer.Physics;
using EMFieldVisualizer.Presentation;
using EMFieldVisualizer.Rendering;
using EMFieldVisualizer.Visualization;

namespace EMFieldVisualizer;

/// <summary>Wires the view model, simulation and renderers together and drives the per-frame loop.</summary>
public partial class MainWindow : Window
{
    private const double ReadoutInterval = 0.15;
    private const double HudInterval = 0.25;

    private readonly MainViewModel _vm = new();
    private readonly Simulation _sim = new();      // what the chosen observer sees
    private readonly Simulation _labSim = new();   // the lab view, for the cross-frame force check
    private readonly OrbitCamera _orbit = new();
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly FrameTimer _frameTimer = new();

    private readonly ParticleRenderer _particles = new();
    private readonly FieldArrowRenderer _fieldArrows = new();
    private readonly ProbeRenderer _probe = new();
    private readonly ForceDiagramRenderer _diagram = new();

    private double _observerTime;
    private double _lastReadout = double.NegativeInfinity, _lastHud = double.NegativeInfinity;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;

        BuildMainScene();
        DiagramVisual.Content = _diagram.Scene;
        _ = new ViewportMouseController(ViewportHost, _orbit, MainCamera, _vm.Probe);

        _orbit.Apply(MainCamera);
        CompositionTarget.Rendering += OnRendering;
        Closed += (_, _) => CompositionTarget.Rendering -= OnRendering;
    }

    private void BuildMainScene()
    {
        var root = new Model3DGroup();
        SceneStyle.AddLights(root);

        // Faint reference axis along the stream direction.
        var axis = new MeshBuilder();
        axis.AddCylinder(new Point3D(-Simulation.Length / 2 - 3, 0, 0), new Point3D(Simulation.Length / 2 + 3, 0, 0), 0.03, 6);
        root.Children.Add(SceneStyle.Model(axis.ToMesh(), MeshBuilder.SolidMaterial(SceneStyle.Axis, 0.3, specular: false)));

        root.Children.Add(_particles.Particles);
        root.Children.Add(_fieldArrows.ElectricArrows);
        root.Children.Add(_fieldArrows.MagneticArrows);
        root.Children.Add(_probe.Group);
        root.Children.Add(_particles.Guides); // translucent: keep last
        SceneVisual.Content = root;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        double now = _stopwatch.Elapsed.TotalSeconds;
        double dt = _frameTimer.Tick(now);

        bool observerMoving = Math.Abs(_vm.ObserverBeta) > 1e-9;
        Configure(_sim, _vm.ObserverBeta);
        Configure(_labSim, 0);
        if (!_vm.Paused)
        {
            _sim.Advance(dt, _vm.AnimationSpeed);
            if (observerMoving) _labSim.Advance(dt, _vm.AnimationSpeed);
            _observerTime += dt;
        }
        _sim.BuildSources();

        _particles.Update(_sim, _vm.ShowParticles);
        _fieldArrows.Update(_sim, _vm);

        var forces = LorentzForce.Compute(_sim, _vm.Probe);
        var forceScale = _vm.AbsoluteForceArrows
            ? ForceScale.AbsoluteTo(ForceReference.Compute(_sim))
            : ForceScale.Normalized;
        _probe.Update(_vm.Probe, forces, forceScale, _vm.ShowProbeForces);
        _diagram.Update(_vm.Probe, forces, forceScale);

        _orbit.Apply(MainCamera);
        ForceDiagramRenderer.SyncCamera(DiagramCamera, _orbit.Forward);
        UpdateClocks();

        if (now - _lastReadout >= ReadoutInterval)
        {
            _lastReadout = now;
            FrameCheck? check = null;
            if (observerMoving)
            {
                // Same probe, computed by a lab observer, to show both frames predict the same motion.
                _labSim.BuildSources();
                check = FrameCheck.Compute(forces, LorentzForce.Compute(_labSim, _vm.Probe), _vm.ObserverBeta, _vm.Relativistic);
            }
            _vm.ProbeText = ProbeReadout.Format(_vm.Probe, forces, forceScale, check);
        }
        if (now - _lastHud >= HudInterval)
        {
            _lastHud = now;
            _vm.HudText = HudFormatter.Status(_frameTimer.Fps, _fieldArrows.ArrowCount, _particles.ParticleCount,
                _vm.ViewModes[_vm.ViewModeIndex], _sim, _vm.Streams);
        }
    }

    private void Configure(Simulation sim, double observerBeta)
    {
        sim.Relativistic = _vm.Relativistic;
        sim.Periodic = _vm.Periodic;
        sim.ContractSpacing = _vm.ContractSpacing;
        sim.ObserverBeta = observerBeta;
        sim.Sync(_vm.Streams);
    }

    private void UpdateClocks()
    {
        const double degreesPerSecond = ParticleRenderer.ClockRevsPerSecond * 360;
        ObserverClockHand.Angle = _observerTime * degreesPerSecond % 360;

        var s = _vm.SelectedStream;
        var line = s == null ? null : _sim.PrimaryLine(s);
        ClockInfo.Text = HudFormatter.ClockInfo(line, _vm.Relativistic);
        if (line == null) return;
        ParticleClockHand.Angle = line.ProperTime * degreesPerSecond % 360;
        ParticleClockFace.Stroke = new SolidColorBrush(SceneStyle.ForKind(line.Species));
    }

    // ---- buttons ----

    private void AddElectron_Click(object sender, RoutedEventArgs e) => _vm.AddStream(ParticleKind.Electron);
    private void AddProton_Click(object sender, RoutedEventArgs e) => _vm.AddStream(ParticleKind.Proton);
    private void AddWire_Click(object sender, RoutedEventArgs e) => _vm.AddStream(ParticleKind.NeutralWire);
    private void Remove_Click(object sender, RoutedEventArgs e) => _vm.RemoveSelected();
    private void MatchVelocity_Click(object sender, RoutedEventArgs e) => _vm.MatchProbeToSelectedStream();
    private void ResetCamera_Click(object sender, RoutedEventArgs e) => _orbit.Reset();
    private void ObserverLab_Click(object sender, RoutedEventArgs e) => _vm.ResetObserver();
    private void ObserverStream_Click(object sender, RoutedEventArgs e) => _vm.RideWithSelectedStream();
    private void ObserverProbe_Click(object sender, RoutedEventArgs e) => _vm.RideWithProbe();
}
