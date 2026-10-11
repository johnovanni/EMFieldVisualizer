namespace EMFieldVisualizer;

/// <summary>Which field quantity the arrows show.</summary>
public enum FieldView
{
    LabFrame = 0,
    PureElectric = 1,
    PureMagnetic = 2,
}

/// <summary>Where the field arrows are sampled.</summary>
public enum SliceMode
{
    Volume = 0,
    YZThroughProbe = 1,
    XYThroughProbe = 2,
    XZThroughProbe = 3,
}
