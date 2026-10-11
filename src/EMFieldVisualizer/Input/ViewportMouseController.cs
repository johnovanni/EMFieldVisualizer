using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using EMFieldVisualizer.Physics;
using EMFieldVisualizer.Rendering;

namespace EMFieldVisualizer.Input;

/// <summary>
/// Mouse handling for the 3D view: left-drag rotates, right/middle-drag pans, the wheel zooms and
/// Shift+left-drag moves the probe in the plane facing the camera.
/// </summary>
public sealed class ViewportMouseController
{
    public static readonly Point3D ProbeMin = new(-20, -25, -25);
    public static readonly Point3D ProbeMax = new(20, 25, 25);

    private enum DragMode { None, Rotate, Pan, Probe }

    private readonly FrameworkElement _host;
    private readonly OrbitCamera _orbit;
    private readonly PerspectiveCamera _camera;
    private readonly Probe _probe;
    private DragMode _drag;
    private Point _last;

    public ViewportMouseController(FrameworkElement host, OrbitCamera orbit, PerspectiveCamera camera, Probe probe)
    {
        _host = host;
        _orbit = orbit;
        _camera = camera;
        _probe = probe;
        host.MouseDown += OnMouseDown;
        host.MouseMove += OnMouseMove;
        host.MouseUp += OnMouseUp;
        host.MouseWheel += (_, e) => _orbit.Zoom(e.Delta);
    }

    /// <summary>
    /// Moves a point by a mouse delta (pixels) within the plane facing the camera, so it follows the cursor,
    /// and clamps it to the allowed probe region.
    /// </summary>
    public static Point3D DragInViewPlane(Point3D position, OrbitCamera orbit, double dx, double dy,
        double viewportWidth, double fieldOfViewDeg)
    {
        double depth = Math.Max(1, Vector3D.DotProduct(position - orbit.Position, orbit.Forward));
        double wpp = OrbitCamera.WorldPerPixel(depth, viewportWidth, fieldOfViewDeg);
        var moved = position + (orbit.Right * dx - orbit.Up * dy) * wpp;
        return new Point3D(
            Math.Clamp(moved.X, ProbeMin.X, ProbeMax.X),
            Math.Clamp(moved.Y, ProbeMin.Y, ProbeMax.Y),
            Math.Clamp(moved.Z, ProbeMin.Z, ProbeMax.Z));
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _host.Focus();
        _last = e.GetPosition(_host);
        _drag = e.ChangedButton switch
        {
            MouseButton.Left => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? DragMode.Probe : DragMode.Rotate,
            MouseButton.Right or MouseButton.Middle => DragMode.Pan,
            _ => DragMode.None,
        };
        if (_drag != DragMode.None) _host.CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_drag == DragMode.None) return;
        var p = e.GetPosition(_host);
        double dx = p.X - _last.X, dy = p.Y - _last.Y;
        _last = p;

        switch (_drag)
        {
            case DragMode.Rotate:
                _orbit.Rotate(dx, dy);
                break;
            case DragMode.Pan:
                _orbit.Pan(dx, dy, _host.ActualWidth, _camera.FieldOfView);
                break;
            case DragMode.Probe:
                _probe.Position = DragInViewPlane(_probe.Position, _orbit, dx, dy, _host.ActualWidth, _camera.FieldOfView);
                break;
        }
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _drag = DragMode.None;
        _host.ReleaseMouseCapture();
    }
}
