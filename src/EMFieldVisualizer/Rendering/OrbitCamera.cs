using System.Windows.Media.Media3D;

namespace EMFieldVisualizer.Rendering;

/// <summary>Camera that orbits a target point (yaw/pitch/distance), with Y up.</summary>
public sealed class OrbitCamera
{
    public const double DefaultYaw = -38;
    public const double DefaultPitch = 22;
    public const double DefaultDistance = 75;
    public const double MinDistance = 3;
    public const double MaxDistance = 600;
    public const double MaxPitch = 89;

    public double Yaw { get; set; } = DefaultYaw;
    public double Pitch { get; set; } = DefaultPitch;
    public double Distance { get; set; } = DefaultDistance;
    public Point3D Target { get; set; } = new(0, 0, 0);

    public void Reset()
    {
        Yaw = DefaultYaw;
        Pitch = DefaultPitch;
        Distance = DefaultDistance;
        Target = new Point3D();
    }

    public static readonly Vector3D WorldUp = new(0, 1, 0);

    /// <summary>Unit vector the camera looks along.</summary>
    public Vector3D Forward
    {
        get
        {
            double y = Yaw * Math.PI / 180, p = Pitch * Math.PI / 180;
            return new Vector3D(-Math.Cos(p) * Math.Sin(y), -Math.Sin(p), -Math.Cos(p) * Math.Cos(y));
        }
    }

    public Vector3D Right
    {
        get
        {
            var r = Vector3D.CrossProduct(Forward, WorldUp);
            r.Normalize();
            return r;
        }
    }

    public Vector3D Up => Vector3D.CrossProduct(Right, Forward);

    public Point3D Position => Target - Forward * Distance;

    public void Rotate(double dxPixels, double dyPixels)
    {
        Yaw -= dxPixels * 0.4;
        Pitch = Math.Clamp(Pitch + dyPixels * 0.4, -MaxPitch, MaxPitch);
    }

    public void Zoom(double wheelDelta) =>
        Distance = Math.Clamp(Distance * Math.Pow(0.88, wheelDelta / 120.0), MinDistance, MaxDistance);

    public void Pan(double dxPixels, double dyPixels, double viewportWidth, double fieldOfViewDeg)
    {
        double worldPerPixel = WorldPerPixel(Distance, viewportWidth, fieldOfViewDeg);
        Target += (-Right * dxPixels + Up * dyPixels) * worldPerPixel;
    }

    public static double WorldPerPixel(double distance, double viewportWidth, double fieldOfViewDeg) =>
        2 * distance * Math.Tan(fieldOfViewDeg * Math.PI / 360) / Math.Max(1, viewportWidth);

    public void Apply(PerspectiveCamera camera)
    {
        camera.Position = Position;
        camera.LookDirection = Forward * Distance;
        camera.UpDirection = WorldUp;
    }
}
