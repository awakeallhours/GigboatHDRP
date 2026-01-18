public struct StabilityProfile
{
    // --- GM (Metacentric Height) ---
    public float GM_Initial;              // GM at small heel (≈ 5–10°)
                                          // Plain: "Initial stability when the boat first starts to lean"

    public bool GM_Initial_Valid;         // Whether GM_Initial is a real computed value
                                          // Plain: "Did we actually measure this, or is it a placeholder?"

    public float GM_Peak;                 // Maximum GM observed during scan
                                          // Plain: "Strongest overall stability the boat showed"

    public float GM_PeakAngle;            // Heel angle where GM_Peak occurs
                                          // Plain: "How far the boat was leaning when stability was strongest"

    public bool GM_Peak_Valid;            // Whether GM_Peak is a real computed value
                                          // Plain: "Did we find a real stability peak?"

    // --- GZ (Righting Arm Curve) ---
    public float GZ_Peak;                 // Maximum righting arm
                                          // Plain: "Strongest righting force pushing the boat upright"

    public float GZ_PeakAngle;            // Heel angle where GZ_Peak occurs
                                          // Plain: "How far the boat was leaning when righting force was strongest"

    public bool GZ_Peak_Valid;            // Whether GZ_Peak is a real computed value
                                          // Plain: "Did we find a real righting‑force peak?"

    public float GZ_ZeroAngle;            // Estimated angle of vanishing stability
                                          // Plain: "The angle where the boat stops being able to right itself"

    public bool GZ_ZeroAngle_Valid;       // Whether zero-crossing was successfully estimated
                                          // Plain: "Did we actually detect the point where stability disappears?"

    // --- COM Guidance ---
    public float COM_SafeMin;             // Lowest recommended COM (vertical)
                                          // Plain: "Lowest safe centre‑of‑mass height"

    public float COM_SafeMax;             // Highest recommended COM (vertical)
                                          // Plain: "Highest safe centre‑of‑mass height"

    // --- Metadata ---
    public float PositiveStabilityRange;  // Degrees of heel where GZ > 0
                                          // Plain: "How far the boat can lean while still being stable"

    public string Notes;                  // Optional: human-readable summary
                                          // Plain: "Extra info about the scan"
}