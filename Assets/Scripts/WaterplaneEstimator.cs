using UnityEngine;

public sealed class WaterplaneEstimator : MonoBehaviour
{
    // Stored geometry bounds (set during Compute)
    private float minZInternal;
    private float maxZInternal;
    private float loaInternal;
    private float sliceLengthInternal;

    // ─────────────────────────────────────────────────────────────
    // CONFIGURATION
    // ─────────────────────────────────────────────────────────────

    [Header("Waterplane Detection")]
    [Tooltip("Percentage of LOA used to determine how deep below the surface a probe can be and still count as part of the waterplane. Typical: 0.01–0.05 (1–5% of LOA).")]
    [SerializeField] private float waterlineDepthPercent = 0.02f;

    [Tooltip("Minimum allowed threshold after scaling (prevents zero or near-zero thresholds on tiny vessels).")]
    [SerializeField] private float minDepthThreshold = 0.05f;

    // ─────────────────────────────────────────────────────────────
    // OUTPUTS
    // ─────────────────────────────────────────────────────────────

    [Header("Computed Waterplane Geometry (Read‑Only)")]
    public float[] sliceBeam;               // Width per slice at waterline
    public float[] sliceArea;               // Area per slice
    public float totalWaterplaneArea;       // Sum of slice areas
    public float LCF;                       // Longitudinal Centre of Flotation (Z)

    // ─────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────

    public float MinZ => minZInternal;
    public float MaxZ => maxZInternal;
    public float LOA => loaInternal;
    public float SliceLength => sliceLengthInternal;

    public void Compute(
        Transform[] samplePoints,
        float[] pointHeights,
        int[] sliceIndices,
        int sliceCount,
        float minZ,
        float maxZ)
    {
        // Store geometry bounds
        minZInternal = minZ;
        maxZInternal = maxZ;

        // Allocate arrays
        sliceBeam = new float[sliceCount];
        sliceArea = new float[sliceCount];

        // Compute LOA
        float LOA = Mathf.Max(0.001f, maxZ - minZ);
        loaInternal = LOA; // <── STORE IT

        // Compute base threshold from LOA
        float baseThreshold = LOA * waterlineDepthPercent;

        // Apply transform scale (depth is measured in world Y)
        float scaledThreshold = Mathf.Max(
            baseThreshold * transform.lossyScale.y,
            minDepthThreshold
        );

        // Slice length
        float sliceLength = LOA / sliceCount;
        sliceLengthInternal = sliceLength; // <── STORE IT

        // Track min/max X per slice
        float[] minX = new float[sliceCount];
        float[] maxX = new float[sliceCount];

        for (int s = 0; s < sliceCount; s++)
        {
            minX[s] = float.MaxValue;
            maxX[s] = float.MinValue;
        }

        // ─────────────────────────────────────────────────────────────
        // PASS 1: Identify waterplane probes per slice
        // ─────────────────────────────────────────────────────────────

        for (int i = 0; i < samplePoints.Length; i++)
        {
            int slice = sliceIndices[i];

            float depth = pointHeights[i] - samplePoints[i].position.y;

            // Only count probes that are submerged but within threshold
            if (depth > 0f && depth <= scaledThreshold)
            {
                float x = samplePoints[i].position.x;

                if (x < minX[slice]) minX[slice] = x;
                if (x > maxX[slice]) maxX[slice] = x;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // PASS 2: Compute beam, area, and LCF
        // ─────────────────────────────────────────────────────────────

        totalWaterplaneArea = 0f;
        float weightedZ = 0f;

        for (int s = 0; s < sliceCount; s++)
        {
            float beam = (maxX[s] > minX[s]) ? (maxX[s] - minX[s]) : 0f;
            sliceBeam[s] = beam;

            float area = beam * sliceLength;
            sliceArea[s] = area;

            totalWaterplaneArea += area;

            float sliceZ = minZ + (s + 0.5f) * sliceLength;
            weightedZ += area * sliceZ;
        }

        LCF = (totalWaterplaneArea > 0f) ? weightedZ / totalWaterplaneArea : 0f;
    }
}