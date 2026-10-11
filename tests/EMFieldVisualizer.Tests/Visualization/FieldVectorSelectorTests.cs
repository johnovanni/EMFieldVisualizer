using EMFieldVisualizer.Visualization;
using static EMFieldVisualizer.Tests.TestHelpers;

namespace EMFieldVisualizer.Tests.Visualization;

public class FieldVectorSelectorTests
{
    // Point 0 is electric-dominated (E=5, cB=3), point 1 is magnetic-dominated (E=3, cB=5).
    private readonly Vector3D[] _e = [new(0, 5, 0), new(0, 3, 0)];
    private readonly Vector3D[] _cb = [new(0, 0, 3), new(0, 0, 5)];
    private readonly Vector3D[] _scratch = new Vector3D[2];

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void LabFrame_PassesThroughSelectedFields(bool showE, bool showB)
    {
        var (e, b) = FieldVectorSelector.Select(FieldView.LabFrame, showE, showB, _e, _cb, 2, _scratch);
        Assert.Same(showE ? _e : null, e);
        Assert.Same(showB ? _cb : null, b);
    }

    [Fact]
    public void PureElectric_ShowsReducedFieldOnlyWhereElectricDominates()
    {
        var (e, b) = FieldVectorSelector.Select(FieldView.PureElectric, false, false, _e, _cb, 2, _scratch);
        Assert.Null(b);
        Assert.NotNull(e);
        AssertClose(new Vector3D(0, 4, 0), e![0]); // √(25 − 9) along E
        Assert.Equal(new Vector3D(), e[1]);
    }

    [Fact]
    public void PureMagnetic_ShowsReducedFieldOnlyWhereMagneticDominates()
    {
        var (e, b) = FieldVectorSelector.Select(FieldView.PureMagnetic, true, true, _e, _cb, 2, _scratch);
        Assert.Null(e);
        Assert.NotNull(b);
        Assert.Equal(new Vector3D(), b![0]);
        AssertClose(new Vector3D(0, 0, 4), b[1]); // √(25 − 9) along B
    }

    [Fact]
    public void EquivalentFields_AreZeroForZeroInput()
    {
        Assert.Equal(new Vector3D(), FieldVectorSelector.EquivalentElectric(new Vector3D(), new Vector3D()));
        Assert.Equal(new Vector3D(), FieldVectorSelector.EquivalentMagnetic(new Vector3D(), new Vector3D()));
    }
}
