using System.Globalization;
using EMFieldVisualizer.Physics;

namespace EMFieldVisualizer.Tests;

internal static class TestHelpers
{
    public static void AssertClose(double expected, double actual, double relativeTolerance = 1e-9, double absoluteTolerance = 1e-12)
    {
        double diff = Math.Abs(expected - actual);
        double allowed = Math.Max(absoluteTolerance, relativeTolerance * Math.Abs(expected));
        Assert.True(diff <= allowed, $"Expected {expected:R} but got {actual:R} (diff {diff:R}, allowed {allowed:R})");
    }

    public static void AssertClose(Vector3D expected, Vector3D actual, double absoluteTolerance = 1e-9)
    {
        Assert.True((expected - actual).Length <= absoluteTolerance, $"Expected {expected} but got {actual}");
    }

    public static void AssertClose(Point3D expected, Point3D actual, double absoluteTolerance = 1e-9)
    {
        Assert.True((expected - actual).Length <= absoluteTolerance, $"Expected {expected} but got {actual}");
    }

    /// <summary>A simulation containing exactly the given streams, with sources already built.</summary>
    public static Simulation SimulationWith(bool periodic, bool relativistic, params ParticleStream[] streams)
    {
        var sim = new Simulation { Periodic = periodic, Relativistic = relativistic };
        sim.Prepare(streams);
        return sim;
    }

    /// <summary>A single particle that sits at x = −L/2 (phase 0, index 0) on the x axis.</summary>
    public static ParticleStream SingleParticle(ParticleKind kind, double beta) =>
        new() { Kind = kind, Beta = beta, Count = 1, OffsetY = 0, OffsetZ = 0 };
}

/// <summary>Runs a test body with the invariant culture so number formatting is predictable.</summary>
public abstract class InvariantCultureTest : IDisposable
{
    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

    protected InvariantCultureTest() => CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        GC.SuppressFinalize(this);
    }
}
