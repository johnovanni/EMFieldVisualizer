namespace EMFieldVisualizer.Physics;

public enum ParticleKind { Electron, Proton, NeutralWire }

/// <summary>
/// A straight stream along the x axis, defined in the lab frame. Either a beam of identical charges, or a
/// <see cref="ParticleKind.NeutralWire"/>: fixed positive ions plus an equal number of electrons drifting at
/// <see cref="Beta"/>, so the wire is neutral in the lab and carries a current when β &gt; 0.
/// </summary>
public sealed class ParticleStream : ObservableObject
{
    private ParticleKind _kind;
    private double _offsetY;
    private double _offsetZ;
    private double _beta = 0.6;
    private int _count = 20;
    private bool _reversed;
    private bool _enabled = true;

    public ParticleKind Kind
    {
        get => _kind;
        set { if (Set(ref _kind, value)) NotifyDerived(); }
    }

    /// <summary>Position of the stream line in the y direction (nm).</summary>
    public double OffsetY
    {
        get => _offsetY;
        set { if (Set(ref _offsetY, value)) NotifyDerived(); }
    }

    /// <summary>Position of the stream line in the z direction (nm).</summary>
    public double OffsetZ
    {
        get => _offsetZ;
        set { if (Set(ref _offsetZ, value)) NotifyDerived(); }
    }

    /// <summary>Lab-frame speed as a fraction of c (for a wire: the electrons' drift speed).</summary>
    public double Beta
    {
        get => _beta;
        set { if (Set(ref _beta, Math.Clamp(value, 0.0, 0.9999))) NotifyDerived(); }
    }

    /// <summary>
    /// Particles in the 40 nm window (lab frame, unless spacing is Lorentz-contracted).
    /// For a wire: the number of ions, and also of electrons.
    /// </summary>
    public int Count
    {
        get => _count;
        set { if (Set(ref _count, Math.Clamp(value, 1, 200))) NotifyDerived(); }
    }

    /// <summary>True = moving (for a wire: electrons drifting) in the −x direction.</summary>
    public bool Reversed
    {
        get => _reversed;
        set { if (Set(ref _reversed, value)) NotifyDerived(); }
    }

    public bool Enabled
    {
        get => _enabled;
        set { if (Set(ref _enabled, value)) NotifyDerived(); }
    }

    public bool IsWire => Kind == ParticleKind.NeutralWire;
    public double Gamma => Phys.Gamma(Beta);

    /// <summary>Charge per particle in units of e (0 for a neutral wire as a whole).</summary>
    public double ChargeSign => Kind switch
    {
        ParticleKind.Electron => -1.0,
        ParticleKind.Proton => 1.0,
        _ => 0.0,
    };

    public double Direction => Reversed ? -1.0 : 1.0;

    /// <summary>Signed lab-frame velocity of the moving particles (beam particles or wire electrons), in c.</summary>
    public double SignedBeta => Direction * Beta;

    public double RestEnergyMeV => Kind == ParticleKind.Proton ? Phys.ProtonRestMeV : Phys.ElectronRestMeV;

    /// <summary>Current carried by a wire (A); the electrons' charge flow. Zero for beams.</summary>
    public double WireCurrent =>
        IsWire ? Count * Phys.ElementaryCharge / (Simulation.Length * Phys.MetresPerUnit) * Beta * Phys.C : 0;

    public string SpeedLabel => IsWire ? "Electron drift speed β (0 = no current)" : "Speed β = v/c";

    public string Summary
    {
        get
        {
            string dir = Reversed ? " (−x)" : "";
            string where = $"@ (y {OffsetY:0.#}, z {OffsetZ:0.#}){(Enabled ? "" : "  [off]")}";
            return IsWire
                ? $"Neutral wire ×{Count}  drift β={Beta:0.####}{dir}  {where}"
                : $"{Kind} ×{Count}  β={Beta:0.####}{dir}  {where}";
        }
    }

    public string RelativityText
    {
        get
        {
            double g = Gamma;
            if (IsWire)
            {
                string flow = Beta == 0
                    ? "No current: a stationary, neutral wire exerts no force at all."
                    : $"Current I = {Phys.Eng(WireCurrent, "A")} (electrons drift {(Reversed ? "−x" : "+x")}, conventional current {(Reversed ? "+x" : "−x")})";
                return $"Ions at rest, electrons drift with γ = {g:0.####}; neutral in the lab.\n{flow}";
            }
            double keEv = (g - 1.0) * RestEnergyMeV * 1e6;
            return $"γ = {g:0.####}   KE = {Phys.Eng(keEv, "eV")}\n" +
                   $"Length contraction: ×{1 / g:0.###}   Clocks tick at ×{1 / g:0.###}";
        }
    }

    private void NotifyDerived()
    {
        OnPropertyChanged(nameof(Gamma));
        OnPropertyChanged(nameof(IsWire));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(RelativityText));
        OnPropertyChanged(nameof(SpeedLabel));
    }
}
