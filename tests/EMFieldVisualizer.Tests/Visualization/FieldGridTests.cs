using EMFieldVisualizer.Physics;
using EMFieldVisualizer.Visualization;

namespace EMFieldVisualizer.Tests.Visualization;

public class FieldGridTests
{
    private static readonly ParticleStream[] CentralStream = [new() { OffsetY = 0, OffsetZ = 0 }];
    private static readonly Point3D Probe = new(3, 4, -5);

    [Fact]
    public void Extent_HasMinimum()
    {
        Assert.Equal(FieldGrid.MinExtent, FieldGrid.ComputeExtent(CentralStream));
        Assert.Equal(FieldGrid.MinExtent, FieldGrid.ComputeExtent([]));
    }

    [Fact]
    public void Extent_GrowsWithOutermostEnabledStream()
    {
        ParticleStream[] streams =
        [
            new() { OffsetY = 12 },
            new() { OffsetZ = -14, Enabled = false },
        ];
        Assert.Equal(12 + FieldGrid.StreamMargin, FieldGrid.ComputeExtent(streams));
    }

    [Fact]
    public void Volume_HasExpectedPointCountAndStaysInBounds()
    {
        var grid = new FieldGrid();
        grid.Build(CentralStream, SliceMode.Volume, 6, Probe);

        // nx = round(6 × 19 / 10) = 11 → 11 × 6 × 6
        Assert.Equal(396, grid.Points.Count);
        Assert.All(grid.Points, p =>
        {
            Assert.InRange(p.X, -grid.XHalfWidth, grid.XHalfWidth);
            Assert.InRange(p.Y, -grid.Extent, grid.Extent);
            Assert.InRange(p.Z, -grid.Extent, grid.Extent);
        });
        Assert.True(grid.CellSize > 0);
    }

    [Fact]
    public void YZSlice_PassesThroughProbeX()
    {
        var grid = new FieldGrid();
        grid.Build(CentralStream, SliceMode.YZThroughProbe, 5, Probe);
        Assert.Equal(11 * 11, grid.Points.Count);
        Assert.All(grid.Points, p => Assert.Equal(Probe.X, p.X));
    }

    [Fact]
    public void XYSlice_PassesThroughProbeZ()
    {
        var grid = new FieldGrid();
        grid.Build(CentralStream, SliceMode.XYThroughProbe, 5, Probe);
        Assert.NotEmpty(grid.Points);
        Assert.All(grid.Points, p => Assert.Equal(Probe.Z, p.Z));
    }

    [Fact]
    public void XZSlice_PassesThroughProbeY()
    {
        var grid = new FieldGrid();
        grid.Build(CentralStream, SliceMode.XZThroughProbe, 5, Probe);
        Assert.NotEmpty(grid.Points);
        Assert.All(grid.Points, p => Assert.Equal(Probe.Y, p.Y));
    }

    [Fact]
    public void Rebuild_ReplacesPreviousPoints()
    {
        var grid = new FieldGrid();
        grid.Build(CentralStream, SliceMode.Volume, 8, Probe);
        grid.Build(CentralStream, SliceMode.YZThroughProbe, 3, Probe);
        Assert.Equal(7 * 7, grid.Points.Count);
    }

    [Theory]
    [InlineData(0, 10, 5, 0, 1.0)]
    [InlineData(0, 10, 5, 4, 9.0)]
    [InlineData(-1, 1, 2, 0, -0.5)]
    public void Center_IsMiddleOfCell(double min, double max, int count, int i, double expected) =>
        Assert.Equal(expected, FieldGrid.Center(min, max, count, i));
}
