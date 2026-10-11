namespace EMFieldVisualizer.Physics;

/// <summary>Physical constants (SI). Scene distances are in nanometres.</summary>
public static class Phys
{
    public const double C = 299_792_458.0;            // m/s
    public const double K = 8.9875517923e9;            // 1/(4 pi eps0), N m^2 / C^2
    public const double ElementaryCharge = 1.602176634e-19; // C
    public const double MetresPerUnit = 1e-9;          // 1 scene unit = 1 nm
    public const double ElectronRestMeV = 0.51099895;
    public const double ProtonRestMeV = 938.27208816;

    public static double Gamma(double beta) => 1.0 / Math.Sqrt(1.0 - beta * beta);

    private static readonly string[] Prefixes = ["y", "z", "a", "f", "p", "n", "µ", "m", "", "k", "M", "G", "T", "P", "E", "Z", "Y"];

    /// <summary>Formats a value with an SI prefix, e.g. 1.44e9 V/m -> "1.44 GV/m".</summary>
    public static string Eng(double value, string unit, int digits = 3)
    {
        if (double.IsNaN(value)) return "—";
        if (value == 0) return $"0 {unit}";
        int exp3 = (int)Math.Floor(Math.Log10(Math.Abs(value)) / 3.0);
        exp3 = Math.Clamp(exp3, -8, 8);
        double scaled = value / Math.Pow(1000, exp3);
        string fmt = "G" + digits;
        return $"{scaled.ToString(fmt)} {Prefixes[exp3 + 8]}{unit}";
    }
}
