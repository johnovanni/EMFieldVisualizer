# Relativistic EM Field Visualizer (WPF, .NET 10)

Interactive 3D view of the electric and magnetic fields around parallel streams of electrons and/or protons.

```
dotnet run --project src/EMFieldVisualizer
dotnet test
```

## Layout
```
src/EMFieldVisualizer/
  Physics/         Simulation (moving charges + exact fields), FieldInvariants, LorentzForce, ParticleStream, Probe, Phys
  Visualization/   FieldGrid (sample points), ArrowScaling, FieldVectorSelector (view modes), FrameTimer   — no WPF UI
  Presentation/    ProbeReadout, HudFormatter (text)
  Rendering/       MeshBuilder, OrbitCamera, SceneStyle, ParticleRenderer, FieldArrowRenderer,
                   ProbeRenderer, ForceDiagramRenderer, ForceArrowSet
  Input/           ViewportMouseController (rotate / pan / zoom / drag probe)
  MainViewModel    UI state, presets;  MainWindow: wiring + per-frame loop only
tests/EMFieldVisualizer.Tests/   xUnit tests (physics checks, layout, formatting, view model, rendering)
```

## Controls
- **Left-drag** rotate · **Right/middle-drag** pan · **Wheel** zoom · **Shift+left-drag** move the probe charge
- Streams panel: presets, add/remove electron or proton streams, per-stream speed (β), particle count, y/z offset, direction
- **Neutral wire** stream type: fixed ions + equally many electrons drifting at β (neutral in the lab, current ∝ β)
- **Observer frame** slider: watch everything from a frame moving at βₒ along x (or ride with a stream / the probe).
  Particle velocities, spacing, contraction, clocks and fields are all transformed; the probe readout converts the
  observer's force back to the lab and compares it with the lab's own calculation (they agree; in classical
  Galilean mode they don't)
- Field display: lab-frame E and/or B, or the **equivalent purely electric / purely magnetic** field
- Probe: charge, position, velocity, plus a force diagram (F_E, F_B, total, velocity) that follows the main camera
- Force arrows: by default length ∝ √(|F| / F_ref), clamped, and brighter = stronger. F_ref is the force on +1e at 5 nm
  from the densest stream (treated as a line charge), so arrows shrink as you move the probe away. Untick
  "Arrow length shows force strength" for normalized arrows (largest always full length: direction and ratios only).

## Physics
- 1 scene unit = 1 nm; every particle carries ±e.
- Each charge moves uniformly, so its field is the exact Heaviside–Feynman field
  `E = kq(1−β²)/(1−β²sin²θ)^{3/2} · R̂/R²`, `B = v×E/c²`, which flattens into a "pancake" as β→1.
  Classical mode drops the relativistic factor.
- Length contraction: particles are drawn squashed by 1/γ along x. Optionally, "fixed proper spacing" packs them γ times closer in the lab frame.
- Time dilation: each particle carries a spinning clock hand, and the HUD compares a lab clock with the selected stream's clock. Moving clocks run at 1/γ of the lab rate.
- Equivalent fields come from the invariants `E² − c²B²` and `E·B`. Where `E·B = 0`, a local frame moving along E×B sees a purely electric field
  `E′ = √(E² − c²B²)` (if E > cB) or a purely magnetic field `B′ = √(B² − E²/c²)` (if cB > E). The probe readout gives that frame's speed.
- Lorentz force on the probe: `F = q(E + v×B)`.
- Observer frames: each line's velocity uses relativistic velocity addition and its density scales by γ(u′)/γ(u),
  so a current-carrying wire becomes charged for an observer moving with its electrons. Dense lines keep the exact
  density by adjusting each drawn particle's charge when rounding to whole particles.
- Periodic streams are the window plus one copy each side, plus the analytic field of the rest of an infinite line
  (`E⊥ = kλ/r·(1 − A/√(A² + r²/γ²))`), so a neutral wire really has E ≈ 0.
