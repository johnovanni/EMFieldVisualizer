using EMFieldVisualizer.Visualization;
using static EMFieldVisualizer.Tests.TestHelpers;

namespace EMFieldVisualizer.Tests.Visualization;

public class FrameTimerTests
{
    [Fact]
    public void FirstTick_ReturnsZero() => Assert.Equal(0, new FrameTimer().Tick(123.4));

    [Fact]
    public void Tick_ReturnsElapsedTime()
    {
        var t = new FrameTimer();
        t.Tick(1.0);
        AssertClose(0.016, t.Tick(1.016));
    }

    [Fact]
    public void Tick_ClampsLongStalls()
    {
        var t = new FrameTimer();
        t.Tick(1.0);
        Assert.Equal(FrameTimer.MaxDelta, t.Tick(5.0));
    }

    [Fact]
    public void Tick_NeverReturnsNegative()
    {
        var t = new FrameTimer();
        t.Tick(1.0);
        Assert.Equal(0, t.Tick(0.5));
    }

    [Fact]
    public void Fps_IsMeasuredOverHalfSecondWindows()
    {
        var t = new FrameTimer();
        Assert.Equal(0, t.Fps);
        for (int i = 0; i <= 50; i++) t.Tick(i * 0.01); // 50 frames over 0.5 s
        AssertClose(100, t.Fps, 1e-6);
    }
}
