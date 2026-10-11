using System.Text;
using EMFieldVisualizer.Physics;

namespace EMFieldVisualizer.Presentation;

/// <summary>Text for the overlay in the 3D view.</summary>
public static class HudFormatter
{
    public static string Status(double fps, int arrowCount, int particleCount, string viewModeName, Simulation sim,
        IEnumerable<ParticleStream> streams)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{fps:0} fps   {arrowCount} field arrows   {particleCount} particles");
        sb.AppendLine($"View: {viewModeName.Split('(')[0].Trim()}   Physics: {(sim.Relativistic ? "relativistic" : "classical")}");
        sb.AppendLine(ObserverDescription(sim.ObserverBeta, sim.Relativistic));
        int idx = 1;
        foreach (var s in streams)
        {
            sb.Append($"#{idx++} ");
            if (!s.Enabled)
            {
                sb.AppendLine($"{Name(s.Kind)}  (off)");
                continue;
            }
            if (s.IsWire)
            {
                sb.Append("wire  ");
                foreach (var line in sim.LinesFor(s))
                    sb.Append($"{(line.Role == LineRole.WireIons ? "ions" : "e⁻")} β={Signed(line.Beta)}  ");
                sb.AppendLine($"(lab current {Phys.Eng(s.WireCurrent, "A")})");
            }
            else if (sim.PrimaryLine(s) is { } line)
            {
                sb.AppendLine($"{Name(s.Kind)} β={Signed(line.Beta)} γ={line.Gamma:0.###}");
            }
        }
        return sb.ToString().TrimEnd();
    }

    public static string ObserverDescription(double observerBeta, bool relativistic)
    {
        if (Math.Abs(observerBeta) < 1e-9) return "Observer: lab frame";
        string gamma = relativistic ? $" (γ = {Phys.Gamma(observerBeta):0.###})" : " (Galilean)";
        return $"Observer: moving at {Signed(observerBeta)}c along x{gamma}";
    }

    public static string ClockInfo(ChargeLine? line, bool relativistic)
    {
        if (line == null) return "";
        if (!relativistic) return "Classical mode: no time dilation or length contraction.";
        if (Math.Abs(line.Beta) < 1e-9)
            return "These particles are at rest for this observer: their clocks keep pace with the observer's clock and they are not contracted.";
        double g = line.Gamma;
        return $"γ = {g:0.###}: for this observer the particles' clocks (white hands on each particle) tick at {1 / g:0.###}× " +
               $"the observer's rate, and each particle is squashed to {1 / g:0.###} of its length along the motion.";
    }

    private static string Name(ParticleKind kind) => kind switch
    {
        ParticleKind.Electron => "e⁻",
        ParticleKind.Proton => "p⁺",
        _ => "wire",
    };

    private static string Signed(double v) => v.ToString("+0.####;−0.####;0");
}
