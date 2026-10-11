using EMFieldVisualizer.Physics;
using static EMFieldVisualizer.Tests.TestHelpers;

namespace EMFieldVisualizer.Tests.Physics;

public class FieldAnalysisTests
{
    [Fact]
    public void PureElectricField_IsUnchanged()
    {
        var fa = FieldAnalysis.Analyze(new Vector3D(3, 0, 0), new Vector3D());
        Assert.True(fa.ElectricDominated);
        AssertClose(9, fa.Invariant);
        AssertClose(3, fa.EPrime);
        AssertClose(0, fa.CBPrime);
        AssertClose(0, fa.BoostBeta);
        AssertClose(1, fa.Purity);
    }

    [Fact]
    public void PureMagneticField_IsUnchanged()
    {
        var fa = FieldAnalysis.Analyze(new Vector3D(), new Vector3D(0, 2, 0));
        Assert.False(fa.ElectricDominated);
        AssertClose(0, fa.EPrime);
        AssertClose(2, fa.CBPrime);
        AssertClose(0, fa.BoostBeta);
    }

    [Fact]
    public void CrossedFields_ElectricDominated_HavePureElectricFrame()
    {
        // This is exactly the lab-frame field of a charge's rest-frame E0 = 4 boosted to β = 0.6 (γ = 1.25):
        // E = γE0 = 5, cB = γβE0 = 3. The analysis must recover E0 and β.
        var fa = FieldAnalysis.Analyze(new Vector3D(5, 0, 0), new Vector3D(0, 3, 0));
        Assert.True(fa.ElectricDominated);
        AssertClose(16, fa.Invariant);
        AssertClose(4, fa.EPrime);
        AssertClose(0, fa.CBPrime, absoluteTolerance: 1e-7);
        AssertClose(0.6, fa.BoostBeta);
        AssertClose(new Vector3D(0, 0, 1), fa.BoostDirection); // along E × B
        AssertClose(1, fa.Purity, absoluteTolerance: 1e-7);
    }

    [Fact]
    public void CrossedFields_MagneticDominated_HavePureMagneticFrame()
    {
        var fa = FieldAnalysis.Analyze(new Vector3D(3, 0, 0), new Vector3D(0, 5, 0));
        Assert.False(fa.ElectricDominated);
        AssertClose(4, fa.CBPrime);
        AssertClose(0, fa.EPrime, absoluteTolerance: 1e-7);
        AssertClose(0.6, fa.BoostBeta); // u = E/(cB)
    }

    [Fact]
    public void ParallelFields_CannotBeMadePure()
    {
        var fa = FieldAnalysis.Analyze(new Vector3D(3, 0, 0), new Vector3D(4, 0, 0));
        AssertClose(3, fa.EPrime);
        AssertClose(4, fa.CBPrime);
        AssertClose(0, fa.BoostBeta);
        AssertClose(0.25, fa.Purity);
    }

    [Fact]
    public void Invariants_AreFrameIndependent()
    {
        // Two lab descriptions of the same rest-frame field E0 seen at different boosts give the same E′.
        foreach (double beta in new[] { 0.1, 0.5, 0.9, 0.99 })
        {
            double g = Phys.Gamma(beta);
            var fa = FieldAnalysis.Analyze(new Vector3D(0, 7 * g, 0), new Vector3D(0, 0, 7 * g * beta));
            AssertClose(7, fa.EPrime, 1e-9);
            AssertClose(beta, fa.BoostBeta, 1e-9);
        }
    }

    [Fact]
    public void ZeroField_IsHandled()
    {
        var fa = FieldAnalysis.Analyze(new Vector3D(), new Vector3D());
        Assert.Equal(0, fa.EPrime);
        Assert.Equal(0, fa.CBPrime);
        Assert.Equal(0, fa.BoostBeta);
        Assert.Equal(1, fa.Purity);
    }
}
