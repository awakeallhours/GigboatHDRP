public struct StabilityProfile
{
    // --- GM (Metacentric Height) ---
    public float GM_Initial;              // GM at small heel (≈ 5–10°)
    public float GM_Peak;                 // Maximum GM observed during scan
    public float GM_PeakAngle;            // Heel angle where GM_Peak occurs

    // --- GZ (Righting Arm Curve) ---
    public float GZ_Peak;                 // Maximum righting arm
    public float GZ_PeakAngle;            // Heel angle where GZ_Peak occurs
    public float GZ_ZeroAngle;            // Estimated angle of vanishing stability

    // --- COM Guidance ---
    public float COM_SafeMin;             // Lowest recommended COM (vertical)
    public float COM_SafeMax;             // Highest recommended COM (vertical)

    // --- Metadata ---
    public float PositiveStabilityRange;  // Degrees of heel where GZ > 0
    public string Notes;                  // Optional: human-readable summary
}
