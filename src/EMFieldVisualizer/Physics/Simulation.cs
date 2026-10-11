using System.Windows.Media.Media3D;

namespace EMFieldVisualizer.Physics;

/// <summary>
/// Moves the charges and evaluates the electromagnetic field they produce, as seen by an observer moving with
/// velocity <see cref="ObserverBeta"/> along x (0 = the lab).
///
/// Each charge moves with constant velocity, so its field is given exactly (retardation included)
/// by the Heaviside–Feynman "present position" form:
///     E = k q (1 − β²) / (1 − β² sin²θ)^(3/2) · R̂ / R²,     B = v × E / c²
/// where R points from the charge's present position to the field point and θ is the angle between
/// R and v. The field is flattened into a "pancake" perpendicular to the motion as β → 1.
/// Because that formula holds in every inertial frame, the observer's fields come from simply re-describing the
/// charges in the observer's frame (their velocities and line densities) — see <see cref="Sync"/>.
/// In classical mode the factor is dropped (Coulomb + Biot–Savart) and frames are related by Galilean transformations.
/// </summary>
public sealed class Simulation
{
    /// <summary>Length of the periodic simulation window along x (nm).</summary>
    public const double Length = 40.0;

    /// <summary>Softening length (nm) to keep fields finite at the particles.</summary>
    public const double Softening = 0.25;

    /// <summary>Upper limit on particles per line.</summary>
    public const int MaxCount = 400;

    /// <summary>
    /// Lines with at least this many particles per window are treated as charge densities: rounding to whole
    /// particles is compensated by scaling each particle's charge. Sparser lines keep exact ±e particles,
    /// because there the individual near-fields matter more than the average density.
    /// </summary>
    public const double DensityScalingThreshold = 8;

    private struct Source
    {
        public double X, Y, Z;  // nm
        public double Q;        // C
        public double BetaX;    // signed velocity along x / c
    }

    /// <summary>A line's continuous remainder beyond the explicit periodic copies.</summary>
    private struct Tail
    {
        public double Y, Z;       // nm
        public double KLambda;    // k · λ in V (with λ in C/nm, distances in nm, ×1e18 folded in)
        public double BetaX;
    }

    private readonly Dictionary<(ParticleStream, LineRole), ChargeLine> _lineMap = [];
    private readonly List<Tail> _tails = [];
    private readonly List<ChargeLine> _lines = [];
    private Source[] _sources = [];
    private int _sourceCount;

    public bool Relativistic { get; set; } = true;
    public bool Periodic { get; set; } = true;
    public bool ContractSpacing { get; set; }

    /// <summary>Observer's velocity along x relative to the lab (c).</summary>
    public double ObserverBeta { get; set; }

    /// <summary>The charge lines of all enabled streams, in stream order (valid after <see cref="Sync"/>).</summary>
    public IReadOnlyList<ChargeLine> Lines => _lines;

    public IEnumerable<ChargeLine> LinesFor(ParticleStream stream) => _lines.Where(l => l.Stream == stream);

    /// <summary>The line that represents a stream's motion and clock (beam, or a wire's electrons).</summary>
    public ChargeLine? PrimaryLine(ParticleStream stream) => _lines.FirstOrDefault(l => l.Stream == stream && l.IsPrimary);

    /// <summary>The probe's (or anything's) lab velocity as seen by the observer.</summary>
    public Vector3D ToObserverVelocity(Vector3D labBeta) => Lorentz.Velocity(labBeta, ObserverBeta, Relativistic);

    /// <summary>
    /// Rebuilds the charge lines from the streams and computes their observer-frame velocity and density,
    /// keeping animation state for lines that already existed.
    /// </summary>
    public void Sync(IEnumerable<ParticleStream> streams)
    {
        var alive = new HashSet<(ParticleStream, LineRole)>();
        _lines.Clear();
        foreach (var s in streams)
        {
            if (!s.Enabled) continue;
            foreach (var (role, labBeta, labCount) in LabLines(s))
            {
                var key = (s, role);
                alive.Add(key);
                if (!_lineMap.TryGetValue(key, out var line)) _lineMap[key] = line = new ChargeLine(s, role);

                line.LabBeta = labBeta;
                line.LabCount = labCount;
                double beta = Lorentz.VelocityAlongX(labBeta, ObserverBeta, Relativistic);
                line.Beta = Math.Clamp(beta, -0.9999, 0.9999); // only reachable in Galilean mode
                line.ExactCount = Lorentz.LineCount(labCount, labBeta, ObserverBeta, Relativistic);
                line.Count = Math.Clamp((int)Math.Round(line.ExactCount), 1, MaxCount);
                double scale = line.ExactCount >= DensityScalingThreshold ? line.ExactCount / line.Count : 1;
                line.ChargePerParticle = line.ChargeSign * Phys.ElementaryCharge * scale;
                _lines.Add(line);
            }
        }
        foreach (var key in _lineMap.Keys.Where(k => !alive.Contains(k)).ToList()) _lineMap.Remove(key);
    }

    /// <summary>The lab-frame lines that make up a stream: (role, signed β, particles per window).</summary>
    public IEnumerable<(LineRole Role, double LabBeta, double LabCount)> LabLines(ParticleStream s)
    {
        if (s.IsWire)
        {
            // Equal lab densities: the wire is neutral in its own (the lab) frame, whatever the drift speed.
            yield return (LineRole.WireIons, 0.0, s.Count);
            yield return (LineRole.WireElectrons, s.SignedBeta, s.Count);
        }
        else
        {
            // With fixed proper spacing, Count is the density in the beam's rest frame: the lab sees γ× more.
            double count = ContractSpacing && Relativistic ? s.Count * s.Gamma : s.Count;
            yield return (LineRole.Beam, s.SignedBeta, count);
        }
    }

    public void Advance(double dt, double unitsPerSecondAtC)
    {
        foreach (var line in _lines)
        {
            double spacing = Length / line.Count;
            double phase = line.Phase + line.Beta * unitsPerSecondAtC * dt / spacing;
            line.Phase = phase - Math.Floor(phase);
            line.ProperTime += Relativistic ? dt / line.Gamma : dt;
        }
    }

    public double ParticleX(ChargeLine line, int index)
    {
        double x = (line.Phase + index) * Length / line.Count;
        x %= Length;
        if (x < 0) x += Length;
        return x - Length / 2;
    }

    public void BuildSources()
    {
        _sourceCount = 0;
        _tails.Clear();
        foreach (var line in _lines)
        {
            _tails.Add(new Tail
            {
                Y = line.OffsetY,
                Z = line.OffsetZ,
                KLambda = Phys.K * line.ChargePerParticle * line.Count / Length / (Phys.MetresPerUnit * Phys.MetresPerUnit),
                BetaX = line.Beta,
            });
            int n = line.Count;
            if (_sources.Length < _sourceCount + n) Array.Resize(ref _sources, Math.Max(_sources.Length * 2, _sourceCount + n));
            for (int i = 0; i < n; i++)
            {
                _sources[_sourceCount++] = new Source
                {
                    X = ParticleX(line, i),
                    Y = line.OffsetY,
                    Z = line.OffsetZ,
                    Q = line.ChargePerParticle,
                    BetaX = line.Beta,
                };
            }
        }
    }

    /// <summary>Convenience: <see cref="Sync"/> then <see cref="BuildSources"/>.</summary>
    public void Prepare(IEnumerable<ParticleStream> streams)
    {
        Sync(streams);
        BuildSources();
    }

    /// <summary>Electric field (V/m) and magnetic field (T) at a point given in nm, in the observer's frame.</summary>
    public void FieldAt(Point3D p, out Vector3D e, out Vector3D b)
    {
        double ex = 0, ey = 0, ez = 0, by = 0, bz = 0;
        int images = Periodic ? 1 : 0;
        const double soft2 = Softening * Softening;
        const double unitScale = 1.0 / (Phys.MetresPerUnit * Phys.MetresPerUnit);

        for (int k = 0; k < _sourceCount; k++)
        {
            ref readonly Source s = ref _sources[k];
            double ry = p.Y - s.Y;
            double rz = p.Z - s.Z;
            double rPerp2 = ry * ry + rz * rz;
            double b2 = s.BetaX * s.BetaX;

            for (int m = -images; m <= images; m++)
            {
                double rx = p.X - (s.X + m * Length);
                double rTrue2 = rx * rx + rPerp2;
                double r2 = rTrue2 + soft2;

                double factor = 1.0;
                if (Relativistic && b2 > 0)
                {
                    // Softening only regularises 1/r²; the angle must use the true geometry.
                    double sin2 = rTrue2 > 0 ? rPerp2 / rTrue2 : 0;
                    double d = 1.0 - b2 * sin2;
                    factor = (1.0 - b2) / (d * Math.Sqrt(d));
                }

                double scale = factor * Phys.K * s.Q * unitScale / (r2 * Math.Sqrt(r2));
                double fx = scale * rx, fy = scale * ry, fz = scale * rz;
                ex += fx; ey += fy; ez += fz;

                // B = v × E / c² with v = βc x̂  →  (β/c)(0, −Ez, Ey)
                by -= s.BetaX * fz / Phys.C;
                bz += s.BetaX * fy / Phys.C;
            }
        }

        if (Periodic) AddTails(p, ref ex, ref ey, ref ez, ref by, ref bz);

        e = new Vector3D(ex, ey, ez);
        b = new Vector3D(0, by, bz);
    }

    /// <summary>
    /// Periodic mode draws the window plus one copy on each side; everything further away is added here as a
    /// continuous uniform line (exact in the continuum limit). For a line moving at β the field of a point charge
    /// is kq(1−β²)R / (x² + r²/γ²)^{3/2}, so a semi-infinite line starting a distance A from the field point gives
    ///     E⊥ = kλ/r · (1 − A/√(A² + a²)),   Eₓ = ∓kλ(1−β²)/√(A² + a²),   a = r/γ.
    /// Without this, a moving and a stationary line of equal density would be truncated differently and a neutral
    /// current-carrying wire would show a spurious electric field.
    /// </summary>
    private void AddTails(Point3D p, ref double ex, ref double ey, ref double ez, ref double by, ref double bz)
    {
        double reach = 1.5 * Length; // the explicit copies cover [−1.5L, 1.5L)
        double aRight = reach - p.X, aLeft = reach + p.X;
        foreach (var t in _tails)
        {
            double ry = p.Y - t.Y, rz = p.Z - t.Z;
            double r = Math.Sqrt(ry * ry + rz * rz);
            double invGamma2 = Relativistic ? 1 - t.BetaX * t.BetaX : 1;
            double a2 = r * r * invGamma2;
            double sRight = Math.Sqrt(aRight * aRight + a2), sLeft = Math.Sqrt(aLeft * aLeft + a2);

            double fx = t.KLambda * invGamma2 * (1 / sLeft - 1 / sRight); // left tail pushes +x, right tail −x
            double fy = 0, fz = 0;
            if (r > 1e-12)
            {
                double perp = t.KLambda / r * ((1 - aRight / sRight) + (1 - aLeft / sLeft)) / r;
                fy = perp * ry;
                fz = perp * rz;
            }
            ex += fx; ey += fy; ez += fz;
            by -= t.BetaX * fz / Phys.C;
            bz += t.BetaX * fy / Phys.C;
        }
    }
}
