using EMFieldVisualizer.Physics;
using static EMFieldVisualizer.Tests.TestHelpers;

namespace EMFieldVisualizer.Tests.Physics;

public class LorentzTests
{
    [Theory]
    [InlineData(0.5, -0.5, 0.8)]   // two 0.5c speeds add to 0.8c, not 1c
    [InlineData(0.5, 0.5, 0.0)]    // riding along
    [InlineData(0.0, 0.6, -0.6)]   // something at rest in the lab moves backwards
    [InlineData(1.0, 0.7, 1.0)]    // light is c in every frame
    public void VelocityAlongX_AddsRelativistically(double beta, double observer, double expected) =>
        AssertClose(expected, Lorentz.VelocityAlongX(beta, observer), 1e-12, 1e-15);

    [Fact]
    public void VelocityAlongX_GalileanSubtracts() =>
        AssertClose(1.1, Lorentz.VelocityAlongX(0.5, -0.6, relativistic: false));

    [Fact]
    public void Velocity_TransverseComponentIsReducedByGamma()
    {
        var v = Lorentz.Velocity(new Vector3D(0, 0.5, 0), 0.6);
        AssertClose(new Vector3D(-0.6, 0.4, 0), v, 1e-12);
    }

    [Fact]
    public void Velocity_LightMovingSidewaysStaysAtC()
    {
        var v = Lorentz.Velocity(new Vector3D(0, 0, 1), 0.6);
        AssertClose(1, v.Length, 1e-12);
    }

    [Fact]
    public void Velocity_GalileanSubtractsObserver() =>
        AssertClose(new Vector3D(-0.6, 0.5, 0), Lorentz.Velocity(new Vector3D(0, 0.5, 0), 0.6, relativistic: false));

    [Fact]
    public void LineCount_RestLineLooksDenserToMovingObserver() =>
        AssertClose(10 * 1.25, Lorentz.LineCount(10, 0, 0.6));

    [Fact]
    public void LineCount_MovingLineLooksSparserWhenRidingAlong() =>
        AssertClose(10 / 1.25, Lorentz.LineCount(10, 0.6, 0.6));

    [Fact]
    public void LineCount_GalileanIsUnchanged() =>
        Assert.Equal(10, Lorentz.LineCount(10, 0.6, 0.3, relativistic: false));

    [Fact]
    public void LineCount_ProperDensityIsFrameIndependent()
    {
        // λ / γ is the density in the line's rest frame: every observer must agree on it.
        double labCount = 17, labBeta = 0.7;
        foreach (double obs in new[] { -0.9, -0.2, 0.3, 0.95 })
        {
            double u = Lorentz.VelocityAlongX(labBeta, obs);
            AssertClose(labCount / Phys.Gamma(labBeta), Lorentz.LineCount(labCount, labBeta, obs) / Phys.Gamma(u), 1e-12);
        }
    }

    [Fact]
    public void Fields_PureElectricGainsMagneticPart()
    {
        var (e, b) = Lorentz.Fields(new Vector3D(0, 1000, 0), new Vector3D(), 0.6);
        AssertClose(new Vector3D(0, 1250, 0), e, 1e-9);
        AssertClose(new Vector3D(0, 0, -1.25 * 0.6 * 1000 / Phys.C), b, 1e-18);
    }

    [Fact]
    public void Fields_ParallelComponentsAreUnchanged()
    {
        var (e, b) = Lorentz.Fields(new Vector3D(7, 0, 0), new Vector3D(3e-6, 0, 0), 0.8);
        AssertClose(new Vector3D(7, 0, 0), e, 1e-12);
        AssertClose(new Vector3D(3e-6, 0, 0), b, 1e-18);
    }

    [Fact]
    public void Fields_PreserveInvariantsAndRoundTrip()
    {
        var e = new Vector3D(120, -340, 560);
        var b = new Vector3D(2e-6, 1e-6, -3e-6);
        var (e2, b2) = Lorentz.Fields(e, b, 0.73);

        double f1 = e.LengthSquared - Phys.C * Phys.C * b.LengthSquared;
        double f2 = e2.LengthSquared - Phys.C * Phys.C * b2.LengthSquared;
        AssertClose(f1, f2, 1e-9);
        AssertClose(Vector3D.DotProduct(e, b), Vector3D.DotProduct(e2, b2), 1e-9);

        var (e3, b3) = Lorentz.Fields(e2, b2, -0.73);
        AssertClose(e, e3, 1e-9);
        AssertClose(b, b3, 1e-15);
    }

    [Fact]
    public void TransverseForceToLab_CoMovingDividesByGamma()
    {
        var f = Lorentz.TransverseForceToLab(new Vector3D(9, 2.5, -5), new Vector3D(), 0.6);
        AssertClose(new Vector3D(0, 2, -4), f, 1e-12);
    }

    [Fact]
    public void TransverseForceToLab_GalileanIsUnchanged() =>
        AssertClose(new Vector3D(0, 2.5, -5), Lorentz.TransverseForceToLab(new Vector3D(9, 2.5, -5), new Vector3D(0.3, 0, 0), 0.6, false));
}
