using EMFieldVisualizer.Input;
using EMFieldVisualizer.Rendering;
using static EMFieldVisualizer.Tests.TestHelpers;

namespace EMFieldVisualizer.Tests.Rendering;

public class OrbitCameraTests
{
    [Fact]
    public void Basis_IsOrthonormal()
    {
        var cam = new OrbitCamera { Yaw = 33, Pitch = -17 };
        AssertClose(1, cam.Forward.Length);
        AssertClose(1, cam.Right.Length);
        AssertClose(1, cam.Up.Length);
        AssertClose(0, Vector3D.DotProduct(cam.Forward, cam.Right), absoluteTolerance: 1e-12);
        AssertClose(0, Vector3D.DotProduct(cam.Forward, cam.Up), absoluteTolerance: 1e-12);
        AssertClose(0, Vector3D.DotProduct(cam.Right, cam.Up), absoluteTolerance: 1e-12);
    }

    [Fact]
    public void Position_IsDistanceBackFromTarget()
    {
        var cam = new OrbitCamera { Target = new Point3D(1, 2, 3), Distance = 10 };
        AssertClose(10, (cam.Position - cam.Target).Length);
        AssertClose(cam.Target - cam.Forward * 10, cam.Position);
    }

    [Fact]
    public void ZeroYawAndPitch_LooksDownMinusZ() =>
        AssertClose(new Vector3D(0, 0, -1), new OrbitCamera { Yaw = 0, Pitch = 0 }.Forward);

    [Fact]
    public void Rotate_ClampsPitch()
    {
        var cam = new OrbitCamera();
        cam.Rotate(0, 10_000);
        Assert.Equal(OrbitCamera.MaxPitch, cam.Pitch);
        cam.Rotate(0, -100_000);
        Assert.Equal(-OrbitCamera.MaxPitch, cam.Pitch);
    }

    [Fact]
    public void Zoom_InIsCloserAndIsClamped()
    {
        var cam = new OrbitCamera();
        double d = cam.Distance;
        cam.Zoom(120);
        Assert.True(cam.Distance < d);
        for (int i = 0; i < 200; i++) cam.Zoom(120);
        Assert.Equal(OrbitCamera.MinDistance, cam.Distance);
        for (int i = 0; i < 200; i++) cam.Zoom(-120);
        Assert.Equal(OrbitCamera.MaxDistance, cam.Distance);
    }

    [Fact]
    public void Pan_MovesTargetWithinViewPlane()
    {
        var cam = new OrbitCamera();
        cam.Pan(50, 20, 800, 50);
        var moved = cam.Target - new Point3D();
        Assert.True(moved.Length > 0);
        AssertClose(0, Vector3D.DotProduct(moved, cam.Forward), absoluteTolerance: 1e-9);
    }

    [Fact]
    public void Reset_RestoresDefaults()
    {
        var cam = new OrbitCamera { Yaw = 1, Pitch = 2, Distance = 3, Target = new Point3D(4, 5, 6) };
        cam.Reset();
        Assert.Equal(OrbitCamera.DefaultYaw, cam.Yaw);
        Assert.Equal(OrbitCamera.DefaultPitch, cam.Pitch);
        Assert.Equal(OrbitCamera.DefaultDistance, cam.Distance);
        Assert.Equal(new Point3D(), cam.Target);
    }

    [Fact]
    public void WorldPerPixel_MatchesHorizontalFieldOfView() =>
        AssertClose(2 * 10.0 / 500, OrbitCamera.WorldPerPixel(10, 500, 90)); // tan 45° = 1

    [Fact]
    public void Apply_SetsPerspectiveCamera()
    {
        var cam = new OrbitCamera();
        var pc = new PerspectiveCamera();
        cam.Apply(pc);
        Assert.Equal(cam.Position, pc.Position);
        AssertClose(cam.Forward * cam.Distance, pc.LookDirection);
        Assert.Equal(OrbitCamera.WorldUp, pc.UpDirection);
    }
}

public class ViewportMouseControllerTests
{
    [Fact]
    public void DragInViewPlane_FollowsCursorWithoutChangingDepth()
    {
        var cam = new OrbitCamera();
        var start = new Point3D(0, 2, 0);

        var moved = ViewportMouseController.DragInViewPlane(start, cam, dx: 40, dy: -10, viewportWidth: 1000, fieldOfViewDeg: 50);

        var delta = moved - start;
        Assert.True(Vector3D.DotProduct(delta, cam.Right) > 0); // dragged right
        Assert.True(Vector3D.DotProduct(delta, cam.Up) > 0);    // dragged up (screen y decreases)
        AssertClose(0, Vector3D.DotProduct(delta, cam.Forward), absoluteTolerance: 1e-9);
    }

    [Fact]
    public void DragInViewPlane_ClampsToProbeRegion()
    {
        var cam = new OrbitCamera();
        var moved = ViewportMouseController.DragInViewPlane(new Point3D(), cam, 1e6, 1e6, 100, 50);
        Assert.InRange(moved.X, ViewportMouseController.ProbeMin.X, ViewportMouseController.ProbeMax.X);
        Assert.InRange(moved.Y, ViewportMouseController.ProbeMin.Y, ViewportMouseController.ProbeMax.Y);
        Assert.InRange(moved.Z, ViewportMouseController.ProbeMin.Z, ViewportMouseController.ProbeMax.Z);
    }
}
