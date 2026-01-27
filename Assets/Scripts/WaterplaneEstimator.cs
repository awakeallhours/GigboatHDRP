using UnityEngine;

public sealed class WaterplaneEstimator : MonoBehaviour
{
    // Stored geometry bounds (set during Compute)
    private float minZInternal;
    private float maxZInternal;
    private float loaInternal;
    private float sliceLengthInternal;

    [Header("Waterplane Detection")]
    [Tooltip("Fraction of hull length used to detect waterplane probes. Lower = stricter.")]
    [SerializeField] private float waterlineDepthPercent = 0.02f;

    [Tooltip("Minimum depth (in world units) for a probe to count as waterplane. Overrides percent if larger.")]
    [SerializeField] private float minDepthThreshold = 0.05f;

    [Header("Computed Waterplane Geometry (Read‑Only)")]
    public float[] sliceBeam;
    public float[] sliceArea;
    public float totalWaterplaneArea;
    public float LCF;

    public float MinZ => minZInternal;
    public float MaxZ => maxZInternal;
    public float LOA => loaInternal;
    public float SliceLength => sliceLengthInternal;

    /// <summary>
    /// Compute waterplane geometry using LOCAL vesselRoot space.
    /// </summary>
    public void Compute(
        Transform vesselRoot,
        Transform[] samplePoints,
        float[] pointHeights,
        int[] sliceIndices,
        int sliceCount,
        float minZ,
        float maxZ)
    {
        minZInternal = minZ;
        maxZInternal = maxZ;

        // Allocate arrays
        sliceBeam = new float[sliceCount];
        sliceArea = new float[sliceCount];

        // Compute LOA in LOCAL space
        float LOA = Mathf.Max(0.001f, maxZInternal - minZInternal);
        loaInternal = LOA;

        // Depth threshold (world Y)
        float baseThreshold = LOA * waterlineDepthPercent;
        float scaledThreshold = Mathf.Max(
            baseThreshold * transform.lossyScale.y,
            minDepthThreshold
        );

        // Slice length in LOCAL space
        float sliceLength = LOA / sliceCount;
        sliceLengthInternal = sliceLength;

        // Track min/max X per slice (LOCAL space)
        float[] minX = new float[sliceCount];
        float[] maxX = new float[sliceCount];

        for (int s = 0; s < sliceCount; s++)
        {
            minX[s] = float.MaxValue;
            maxX[s] = float.MinValue;
        }

        // ─────────────────────────────────────────────
        // PASS 1: Identify waterplane probes per slice
        // ─────────────────────────────────────────────
        for (int i = 0; i < samplePoints.Length; i++)
        {
            int slice = sliceIndices[i];

            // Convert probe position to LOCAL vesselRoot space
            Vector3 local = vesselRoot.InverseTransformPoint(samplePoints[i].position);

            float depth = pointHeights[i] - samplePoints[i].position.y;

            if (depth > 0f && depth <= scaledThreshold)
            {
                float x = local.x;

                if (x < minX[slice]) minX[slice] = x;
                if (x > maxX[slice]) maxX[slice] = x;
            }
        }

        // ─────────────────────────────────────────────
        // PASS 2: Compute beam, area, and LCF
        // ─────────────────────────────────────────────
        totalWaterplaneArea = 0f;
        float weightedZ = 0f;

        for (int s = 0; s < sliceCount; s++)
        {
            float beam = (maxX[s] > minX[s]) ? (maxX[s] - minX[s]) : 0f;
            sliceBeam[s] = beam;

            float area = beam * sliceLength;
            sliceArea[s] = area;

            totalWaterplaneArea += area;

            float sliceZ = minZInternal + (s + 0.5f) * sliceLength;
            weightedZ += area * sliceZ;
        }

        LCF = (totalWaterplaneArea > 0f) ? weightedZ / totalWaterplaneArea : 0f;

        /*    Debug.Log(
        $"[Estimator Debug]\n" +
        $"Input minZ={minZ}, maxZ={maxZ}\n" +
        $"Internal minZ={minZInternal}, maxZInternal={maxZInternal}\n" +
        $"LOA={loaInternal}, SliceLength={sliceLengthInternal}");*/
    }
}