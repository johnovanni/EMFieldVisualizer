using EMFieldVisualizer.Physics;

namespace EMFieldVisualizer.Tests.Physics;

public class ProbeTests
{
    [Fact]
    public void Beta_BelowLightSpeedIsUnchanged()
    {
        var p = new Probe { BetaX = 0.3, BetaY = -0.4, BetaZ = 0 };
        TestHelpers.AssertClose(new Vector3D(0.3, -0.4, 0), p.Beta);
    }

    [Fact]
    public void Beta_IsClampedToBelowOneKeepingDirection()
    {
        var p = new Probe { BetaX = 0.9, BetaY = 0.9, BetaZ = 0 };
        var b = p.Beta;
        TestHelpers.AssertClose(0.999, b.Length);
        TestHelpers.AssertClose(b.X, b.Y);
    }

    [Fact]
    public void Position_RoundTripsThroughComponents()
    {
        var p = new Probe { Position = new Point3D(1, 2, 3) };
        Assert.Equal(1, p.X);
        Assert.Equal(2, p.Y);
        Assert.Equal(3, p.Z);
        Assert.Equal(new Point3D(1, 2, 3), p.Position);
    }
}
