using Axiom.Vessel.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class StabilityProfileBuilder
{
    public static StabilityProfile Build(
        IReadOnlyList<StabilitySample> samples,
        float comSafeMin,
        float comSafeMax,
        string notes = "")
    {
        // ------------------------------------------------------------
        // 1. Initial GM (first heel >= 5°)
        // ------------------------------------------------------------
        bool initialFound = samples.Any(s => s.HeelDeg >= 5f);

        var initial = initialFound
            ? samples.OrderBy(s => s.HeelDeg).First(s => s.HeelDeg >= 5f)
            : default;

        bool initialValid = initialFound && initial.GM > 0f;


        // ------------------------------------------------------------
        // 2. GM Peak (largest GM value)
        // ------------------------------------------------------------
        var gmPeak = samples
            .OrderByDescending(s => s.GM)
            .FirstOrDefault();

        bool gmPeakValid = gmPeak.GM > 0f;


        // ------------------------------------------------------------
        // 3. GZ Peak (largest righting arm)
        // ------------------------------------------------------------
        var gzPeak = samples
            .OrderByDescending(s => s.GZ)
            .FirstOrDefault();

        bool gzPeakValid = gzPeak.GZ > 0f;


        // ------------------------------------------------------------
        // 4. Angle of Vanishing Stability (GZ crosses zero)
        // ------------------------------------------------------------
        float zeroAngle = EstimateZeroCrossing(samples);
        bool zeroValid = zeroAngle > 0f;


        // ------------------------------------------------------------
        // 5. Positive Stability Range (same as zeroAngle)
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

            // COM (placeholder for now)
            COM_SafeMin = comSafeMin,
            COM_SafeMax = comSafeMax,

            // Metadata
            PositiveStabilityRange = positiveRange,
            Notes = notes
        };
    }


    // ------------------------------------------------------------
    // Zero-crossing estimator (unchanged)
    // ------------------------------------------------------------
    private static float EstimateZeroCrossing(IReadOnlyList<StabilitySample> samples)
    {
        for (int i = 1; i < samples.Count; i++)
        {
            var prev = samples[i - 1];
            var curr = samples[i];

            if (prev.GZ > 0f && curr.GZ <= 0f)
            {
                float t = prev.GZ / (prev.GZ - curr.GZ);
                return Mathf.Lerp(prev.HeelDeg, curr.HeelDeg, t);
            }
        }

        return samples.Last().HeelDeg;
    }
}
