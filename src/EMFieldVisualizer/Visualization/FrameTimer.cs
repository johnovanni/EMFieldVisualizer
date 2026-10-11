namespace EMFieldVisualizer.Visualization;

/// <summary>Turns absolute timestamps into clamped frame deltas and a smoothed frames-per-second value.</summary>
public sealed class FrameTimer
{
    /// <summary>Longest step allowed, so a stall (e.g. dragging the window) does not make particles jump.</summary>
    public const double MaxDelta = 0.1;

    private const double FpsWindow = 0.5;

    private double _last = double.NaN;
    private double _windowStart;
    private int _frames;

    public double Fps { get; private set; }

    /// <summary>Records a frame at <paramref name="nowSeconds"/> and returns the clamped time since the previous one.</summary>
    public double Tick(double nowSeconds)
    {
        if (double.IsNaN(_last))
        {
            _last = _windowStart = nowSeconds;
            return 0;
        }
        double dt = Math.Clamp(nowSeconds - _last, 0, MaxDelta);
        _last = nowSeconds;

        _frames++;
        if (nowSeconds - _windowStart >= FpsWindow)
        {
            Fps = _frames / (nowSeconds - _windowStart);
            _frames = 0;
            _windowStart = nowSeconds;
        }
        return dt;
    }
}
