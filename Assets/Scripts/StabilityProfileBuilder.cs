using System.Collections.Generic;
using System.Linq;

public static class StabilityProfileBuilder
{
    public static StabilityProfile Build(
        IReadOnlyList<StabilitySample> samples,
        float comSafeMin,
        float comSafeMax,
        string notes = "")
    {
        // --- 1. Initial GM (smallest heel above threshold) ---
        var initial = samples
            .OrderBy(s => s.Heel)
            .First(s => s.Heel >= 5f);   // You can expose this threshold if needed

        // --- 2. GM Peak ---
        var gmPeak = samples
            .OrderByDescending(s => s.GM)
            .First();

        // --- 3. GZ Peak ---
        var gzPeak = samples
            .OrderByDescending(s => s.GZ)
            .First();

        // --- 4. Angle of Vanishing Stability (GZ crosses zero) ---
        float zeroAngle = EstimateZeroCrossing(samples);

        // --- 5. Positive Stability Range ---
        float positiveRange = zeroAngle;

        // --- 6. Assemble final profile ---
        return new StabilityProfile
        {
            GM_Initial = initial.GM,
            GM_Peak = gmPeak.GM,
            GM_PeakAngle = gmPeak.Heel,

            GZ_Peak = gzPeak.GZ,
            GZ_PeakAngle = gzPeak.Heel,
            GZ_ZeroAngle = zeroAngle,

            COM_SafeMin = comSafeMin,
            COM_SafeMax = comSafeMax,

            PositiveStabilityRange = positiveRange,
            Notes = notes
        };
    }

    private static float EstimateZeroCrossing(IReadOnlyList<StabilitySample> samples)
    {
        // Find first sample where GZ becomes negative or near-zero
        for (int i = 1; i < samples.Count; i++)
        {
            var prev = samples[i - 1];
            var curr = samples[i];

            if (prev.GZ > 0f && curr.GZ <= 0f)
            {
                // Linear interpolation (no magic numbers)
                float t = prev.GZ / (prev.GZ - curr.GZ);
                return Mathf.Lerp(prev.Heel, curr.Heel, t);
            }
        }

        // If never crosses zero, return last heel angle
        return samples.Last().Heel;
    }
}
