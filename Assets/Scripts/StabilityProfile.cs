/// <summary>
/// Represents the interpreted results of a GM/GZ stability scan.
/// 
/// This struct contains:
/// - GM characteristics (initial stability, peak stability)
/// - GZ characteristics (righting‑arm peak, vanishing‑stability angle)
/// - COM safety guidance
/// - Metadata describing the scan
/// 
/// All fields are populated by <see cref="StabilityProfileBuilder"/>.
/// The scanner itself only produces raw samples.
/// </summary>
public struct StabilityProfile
{
    // --------------------------------------------------------------------
    // GM (Metacentric Height)
    // --------------------------------------------------------------------

    /// <summary>
    /// GM at small heel (typically the first heel ≥ 5°).
    /// Represents the vessel's initial stability when it first begins to lean.
    /// </summary>
    public float GM_Initial;

    /// <summary>
    /// True if <see cref="GM_Initial"/> is a real computed value.
    /// False if no suitable sample existed (e.g., insufficient heel range).
    /// </summary>
    public bool GM_Initial_Valid;

    /// <summary>
    /// Maximum GM observed during the scan.
    /// Represents the strongest overall stability the vessel demonstrated.
    /// </summary>
    public float GM_Peak;

    /// <summary>
    /// Heel angle (in degrees) at which <see cref="GM_Peak"/> occurred.
    /// </summary>
    public float GM_PeakAngle;

    /// <summary>
    /// True if <see cref="GM_Peak"/> is a real computed value.
    /// False if GM never became positive.
    /// </summary>
    public bool GM_Peak_Valid;


    // --------------------------------------------------------------------
    // GZ (Righting Arm Curve)
    // --------------------------------------------------------------------

    /// <summary>
    /// Maximum righting arm (GZ) observed during the scan.
    /// Represents the strongest righting force pushing the vessel upright.
    /// </summary>
    public float GZ_Peak;

    /// <summary>
    /// Heel angle (in degrees) at which <see cref="GZ_Peak"/> occurred.
    /// </summary>
    public float GZ_PeakAngle;

    /// <summary>
    /// True if <see cref="GZ_Peak"/> is a real computed value.
    /// False if GZ never became positive.
    /// </summary>
    public bool GZ_Peak_Valid;

    /// <summary>
    /// Estimated angle (in degrees) where GZ crosses zero.
    /// Represents the angle of vanishing stability — beyond this, the vessel
    /// can no longer right itself.
    /// </summary>
    public float GZ_ZeroAngle;

    /// <summary>
    /// True if a zero‑crossing was successfully detected.
    /// False if GZ never crossed zero within the scanned range.
    /// </summary>
    public bool GZ_ZeroAngle_Valid;


    // --------------------------------------------------------------------
    // COM Guidance
    // --------------------------------------------------------------------

    /// <summary>
    /// Lowest recommended safe centre‑of‑mass height for this vessel.
    /// Provided by the COM subsystem.
    /// </summary>
    public float COM_SafeMin;

    /// <summary>
    /// Highest recommended safe centre‑of‑mass height for this vessel.
    /// Provided by the COM subsystem.
    /// </summary>
    public float COM_SafeMax;


    // --------------------------------------------------------------------
    // Metadata
    // --------------------------------------------------------------------

    /// <summary>
    /// Range of heel angles (in degrees) where GZ remains positive.
    /// Represents how far the vessel can lean while still being self‑righting.
    /// </summary>
    public float PositiveStabilityRange;

    /// <summary>
    /// Optional human‑readable notes describing the scan.
    /// Useful for UI, debugging, or automated reporting.
    /// </summary>
    public string Notes;
}