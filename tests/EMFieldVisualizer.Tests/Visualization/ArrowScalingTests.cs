using EMFieldVisualizer.Visualization;
using static EMFieldVisualizer.Tests.TestHelpers;

namespace EMFieldVisualizer.Tests.Visualization;

public class ArrowScalingTests
{
    [Fact]
    public void ReferenceMagnitude_IsPercentileOfNonZeroMagnitudes()
    {
        var v = Enumerable.Range(1, 10).Select(i => new Vector3D(i, 0, 0)).Append(new Vector3D()).ToArray();
        // 10 non-zero values; index (int)(0.85 × 9) = 7 → 8
        Assert.Equal(8, ArrowScaling.ReferenceMagnitude(v, v.Length));
    }

    [Fact]
    public void ReferenceMagnitude_OnlyLooksAtFirstCountItems()
    {
        Vector3D[] v = [new(1, 0, 0), new(100, 0, 0)];
        Assert.Equal(1, ArrowScaling.ReferenceMagnitude(v, 1));
    }

    [Fact]
    public void ReferenceMagnitude_IsZeroForNullOrEmpty()
    {
        Assert.Equal(0, ArrowScaling.ReferenceMagnitude(null, 5));
        Assert.Equal(0, ArrowScaling.ReferenceMagnitude([new Vector3D()], 1));
    }

    [Fact]
    public void WithFloor_KeepsNearlyCancelledFieldsSmall()
    {
        Assert.Equal(ArrowScaling.ReferenceFloorFraction * 1000, ArrowScaling.WithFloor(1, 1000));
        Assert.Equal(500, ArrowScaling.WithFloor(500, 1000));
        Assert.Equal(3, ArrowScaling.WithFloor(3, 0));
    }

    [Theory]
    [InlineData(0, 5, 5)]      // no history
    [InlineData(5, 0, 0)]      // field vanished
    [InlineData(1, 10, 10)]    // big jump: snap
    [InlineData(10, 1, 1)]
    public void Smooth_SnapsWhenAppropriate(double previous, double target, double expected) =>
        Assert.Equal(expected, ArrowScaling.Smooth(previous, target));

    [Fact]
    public void Smooth_EasesSmallChanges()
    {
        double s = ArrowScaling.Smooth(1.0, 2.0);
        AssertClose(1.15, s);
    }

    [Fact]
    public void Length_ReferenceMagnitudeFillsCellFraction() =>
        AssertClose(ArrowScaling.CellFill * 2, ArrowScaling.Length(5, 5, cellSize: 2, userScale: 1));

    [Fact]
    public void Length_IsSqrtCompressed() =>
        AssertClose(ArrowScaling.CellFill * 0.5, ArrowScaling.Length(0.25, 1, 1, 1));

    [Fact]
    public void Length_IsCapped() =>
        AssertClose(ArrowScaling.CellFill * ArrowScaling.MaxRelativeLength, ArrowScaling.Length(1e9, 1, 1, 1));

    [Fact]
    public void Length_ScalesWithUserScale() =>
        AssertClose(2 * ArrowScaling.Length(1, 1, 1, 1), ArrowScaling.Length(1, 1, 1, 2));

    [Theory]
    [InlineData(1e-6, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public void Length_IsZeroWhenTooSmallOrUndefined(double magnitude, double reference) =>
        Assert.Equal(0, ArrowScaling.Length(magnitude, reference, 1, 1));

    [Fact]
    public void ColorCoordinate_IsMonotonicAndClamped()
    {
        double[] mags = [1e-6, 0.01, 0.1, 1, 10, 1e6];
        var u = mags.Select(m => ArrowScaling.ColorCoordinate(m, 1)).ToArray();
        for (int i = 1; i < u.Length; i++) Assert.True(u[i] >= u[i - 1]);
        Assert.Equal(0.02, u[0]);
        Assert.Equal(0.98, u[^1]);
        Assert.Equal(0.02, ArrowScaling.ColorCoordinate(0, 1));
    }
}
