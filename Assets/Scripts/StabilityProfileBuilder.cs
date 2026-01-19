using Axiom.Vessel.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Constructs a <see cref="StabilityProfile"/> from a sequence of raw GM/GZ samples.
/// 
/// This builder is responsible ONLY for interpreting the sampled data:
/// - Identifying GM_Initial (first meaningful GM value)
/// - Identifying GM_Peak (maximum GM)
/// - Identifying GZ_Peak (maximum righting arm)
/// - Estimating the angle of vanishing stability (GZ = 0)
/// - Computing the positive stability range
/// - Embedding COM safe limits and metadata
/// 
/// IMPORTANT:
/// This class does NOT compute GM or GZ. It assumes the scanner has already
/// produced physically correct samples using the vessel's orientation profile
/// and <see cref="GMGZUtility"/>.
/// </summary>
public static class StabilityProfileBuilder
{
    /// <summary>
    /// Builds a fully interpreted <see cref="StabilityProfile"/> from raw stability samples.
    /// </summary>
    /// <param name="samples">List of GM/GZ samples across heel angles.</param>
    /// <param name="comSafeMin">Lower safe COM height (vessel-specific).</param>
    /// <param name="comSafeMax">Upper safe COM height (vessel-specific).</param>
    /// <param name="notes">Optional metadata string describing the scan.</param>
    /// <returns>A populated <see cref="StabilityProfile"/> summarising vessel stability.</returns>
    public static StabilityProfile Build(
        IReadOnlyList<StabilitySample> samples,
        float comSafeMin,
        float comSafeMax,
        string notes = "")
    {
        // ------------------------------------------------------------
        // 1. GM_Initial
        //    Definition: GM at the first heel with |heel| >= 5°.
        //    Rationale: GM is unstable/noisy at very small angles.
        //    NOTE: Uses ABS(heel) so it works for negative-only scans.
        // ------------------------------------------------------------
        bool initialFound = samples.Any(s => Mathf.Abs(s.HeelDeg) >= 5f);

        var initial = initialFound
            ? samples
                .OrderBy(s => Mathf.Abs(s.HeelDeg))
                .First(s => Mathf.Abs(s.HeelDeg) >= 5f)
            : default;

        bool initialValid = initialFound && initial.GM > 0f;


        // ------------------------------------------------------------
        // 2. GM_Peak
        //    Definition: Maximum GM across all samples.
        //    Valid only if GM is positive.
        // ------------------------------------------------------------
        var gmPeak = samples
            .OrderByDescending(s => s.GM)
            .FirstOrDefault();

        bool gmPeakValid = gmPeak.GM > 0f;


        // ------------------------------------------------------------
        // 3. GZ_Peak
        //    Definition: Maximum righting arm (GZ).
        //    Valid only if GZ is positive.
        // ------------------------------------------------------------
        var gzPeak = samples
            .OrderByDescending(s => s.GZ)
            .FirstOrDefault();

        bool gzPeakValid = gzPeak.GZ > 0f;


        // ------------------------------------------------------------
        // 4. Angle of Vanishing Stability
        //    Definition: Heel angle where GZ crosses zero.
        //    Implementation:
        //      - Detects sign change: GZ > 0 → GZ <= 0
        //      - Linearly interpolates heel angle at crossing
        //      - Returns ABS(heel) so the result is a magnitude
        // ------------------------------------------------------------
        float zeroAngle = EstimateZeroCrossingMagnitude(samples);

        // Valid only if the zero-crossing magnitude is positive.
        bool zeroValid = zeroAngle > 0f;


        // ------------------------------------------------------------
        // 5. Positive Stability Range
        //    Definition: Range of heel angles where GZ > 0.
        //    For now, this is simply the zero-crossing magnitude.
        // ------------------------------------------------------------
        float positiveRange = zeroAngle;


        // ------------------------------------------------------------
        // 6. Assemble final profile
        // ------------------------------------------------------------
        return new StabilityProfile
        {
            // GM
            GM_Initial = initialFound ? initial.GM : 0f,
            GM_Initial_Valid = initialValid,

            GM_Peak = gmPeak.GM,
            GM_PeakAngle = gmPeak.HeelDeg,
            GM_Peak_Valid = gmPeakValid,

            // GZ
            GZ_Peak = gzPeak.GZ,
            GZ_PeakAngle = gzPeak.HeelDeg,
            GZ_Peak_Valid = gzPeakValid,

            GZ_ZeroAngle = zeroAngle,
            GZ_ZeroAngle_Valid = zeroValid,

            // COM safe band
            COM_SafeMin = comSafeMin,
            COM_SafeMax = comSafeMax,

            // Metadata
            PositiveStabilityRange = positiveRange,
            Notes = notes
        };
    }


    // ------------------------------------------------------------
    // Zero-crossing estimator (magnitude)
    // ------------------------------------------------------------

    /// <summary>
    /// Estimates the heel angle magnitude where GZ crosses zero by linearly
    /// interpolating between the last positive GZ sample and the first
    /// non-positive sample.
    /// 
    /// Returns the ABSOLUTE heel angle at the crossing, so the result is
    /// always a positive magnitude.
    /// 
    /// If no crossing is found, returns the absolute heel angle of the
    /// final sample.
    /// </summary>
    private static float EstimateZeroCrossingMagnitude(IReadOnlyList<StabilitySample> samples)
    {
        for (int i = 1; i < samples.Count; i++)
        {
            var prev = samples[i - 1];
            var curr = samples[i];

            // Detect sign change: GZ > 0 → GZ <= 0
            if (prev.GZ > 0f && curr.GZ <= 0f)
            {
                // Linear interpolation factor between prev and curr
                float t = prev.GZ / (prev.GZ - curr.GZ);

                // Interpolate heel angle at zero-crossing
                float heelAtZero = Mathf.Lerp(prev.HeelDeg, curr.HeelDeg, t);

                // Return magnitude so we don't propagate sign into stability range
                return Mathf.Abs(heelAtZero);
            }
        }

        // No zero-crossing found — return magnitude of last heel angle.
        return Mathf.Abs(samples.Last().HeelDeg);
    }
}