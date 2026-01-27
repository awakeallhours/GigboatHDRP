using UnityEngine;
using Axiom.Physics.Units;
using Axiom.Vessel.Diagnostics;

[DisallowMultipleComponent]
public sealed class Buoyancy : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // NOTES (PER‑VESSEL)
    // ─────────────────────────────────────────────────────────────
    [Header("Notes")]
    [TextArea(3, 6)]
    [SerializeField] private string notes;

    // ─────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private Rigidbody rb;

    [Tooltip("Root transform of the vessel (defines local hull space).")]
    [SerializeField] private Transform vesselRoot;

    [SerializeField] private MonoBehaviour waterSurfaceSource;
    private IWaterSurface waterSurface;

    [SerializeField] private BoatCOB boatCOB;
    [SerializeField] private WaterProbeSampler probeSampler;
    [SerializeField] private WaterplaneEstimator waterplaneEstimator;

    // ─────────────────────────────────────────────────────────────
    // GLOBAL BUOYANCY CONTROL
    // ─────────────────────────────────────────────────────────────
    [Header("Probe Type Scaling")]
    [SerializeField] private float buoyancyScale = 1f;

    [SerializeField] private float keelBuoyancyScale = 1f;
    [SerializeField] private float sideBuoyancyScale = 0.4f;

    [SerializeField] private float keelRightingScale = 1f;
    [SerializeField] private float sideRightingScale = 0.2f;

    // ─────────────────────────────────────────────────────────────
    // BASE SETTINGS
    // ─────────────────────────────────────────────────────────────
    [Header("Settings")]
    [SerializeField] private float buoyancyStrength = 10f;
    [SerializeField] private float waterDrag = 1f;
    [SerializeField] private float waterAngularDrag = 0.5f;

    // ─────────────────────────────────────────────────────────────
    // HEAVE DAMPING
    // ─────────────────────────────────────────────────────────────
    [Header("Heave Damping")]
    [SerializeField] private float heaveDampingStrength = 2000f;

    // ─────────────────────────────────────────────────────────────
    // HYBRID BUOYANCY (SI‑CLEAN)
    // ─────────────────────────────────────────────────────────────
    [Header("Hybrid Buoyancy (SI Clean)")]
    [SerializeField] private DensityValue waterDensity;
    [SerializeField] private AreaValue probeArea;
    [SerializeField] private bool autoComputeStrength = true;

    [SerializeField] private bool enableRightingMoment = true;
    [SerializeField] private float rightingStrength = 0.5f;

    // ─────────────────────────────────────────────────────────────
    // STERN IMMERSION
    // ─────────────────────────────────────────────────────────────
    [Header("Stern Immersion")]
    [SerializeField] private int sternProbeIndex = 0;
    [SerializeField] private DistanceValue sternReferenceDepth;

    public float SternImmersion01
    {
        get; private set;
    }

    // ─────────────────────────────────────────────────────────────
    // PUBLIC ACCESSORS
    // ─────────────────────────────────────────────────────────────
    public DensityValue WaterDensity => waterDensity;
    public float BuoyancyStrength => buoyancyStrength;

    public Transform[] SamplePoints => samplePoints;
    public float[] PointHeights => pointHeights;
    public bool[] PointValid => pointValid;
    public ProbeType[] ProbeTypes => probeTypes;

    public float TotalBuoyancyForce => totalBuoyancyForce;

    public AreaValue ProbeArea
    {
        get => probeArea;
        set => probeArea = value;
    }

    // ─────────────────────────────────────────────────────────────
    // INTERNAL PROBE DATA
    // ─────────────────────────────────────────────────────────────
    private bool[] pointValid;
    private float[] pointHeights;
    private Vector3[] pointNormals;
    private Transform[] samplePoints;
    private ProbeType[] probeTypes;

    // ─────────────────────────────────────────────────────────────
    // ACCUMULATORS
    // ─────────────────────────────────────────────────────────────
    private Vector3 cobSumLocal;
    private float totalBuoyancyForce;
    private float totalSubmergedVolume;

    public bool debugBuoyancy = false;

    // Z‑SLICE DATA (LOCAL SPACE)
    private int[] sliceCounts;
    private int[] sliceIndices;
    private float minZ;
    private float maxZ;
    private int sliceCount;

    // ─────────────────────────────────────────────────────────────
    // UNITY EVENTS
    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (boatCOB == null)
            boatCOB = GetComponent<BoatCOB>();

        if (waterplaneEstimator == null)
            waterplaneEstimator = GetComponent<WaterplaneEstimator>();

        if (vesselRoot == null)
            vesselRoot = transform;

        // Auto‑assign water surface
        var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in behaviours)
        {
            if (mb is IWaterSurface ws)
            {
                waterSurfaceSource = mb;
                waterSurface = ws;
                break;
            }
        }

        if (probeSampler == null)
            probeSampler = FindFirstObjectByType<WaterProbeSampler>();
    }

    private void Start()
    {
        if (probeSampler == null)
        {
            Debug.LogError("Buoyancy: No WaterProbeSampler found in scene.");
            return;
        }

        probeSampler.GetProbeData(
            out pointValid,
            out pointHeights,
            out pointNormals,
            out samplePoints,
            out probeTypes
        );

        // ─────────────────────────────────────────────
        // LOCAL‑SPACE Z RANGE
        // ─────────────────────────────────────────────
        minZ = float.MaxValue;
        maxZ = float.MinValue;

        for (int i = 0; i < samplePoints.Length; i++)
        {
            float zLocal = vesselRoot.InverseTransformPoint(samplePoints[i].position).z;
            if (zLocal < minZ) minZ = zLocal;
            if (zLocal > maxZ) maxZ = zLocal;
        }

        // Infer slice count (sqrt(N) heuristic)
        sliceCount = probeTypes.Length > 0
            ? Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(probeTypes.Length)))
            : 1;

        sliceCounts = new int[sliceCount];
        sliceIndices = new int[samplePoints.Length];

        // ─────────────────────────────────────────────
        // ASSIGN PROBES TO LOCAL‑SPACE SLICES
        // ─────────────────────────────────────────────
        for (int i = 0; i < samplePoints.Length; i++)
        {
            float zLocal = vesselRoot.InverseTransformPoint(samplePoints[i].position).z;
            float t = Mathf.InverseLerp(minZ, maxZ, zLocal);
            int slice = Mathf.Clamp(Mathf.FloorToInt(t * sliceCount), 0, sliceCount - 1);

            sliceIndices[i] = slice;
            sliceCounts[slice]++;
        }

        // ─────────────────────────────────────────────
        // COMPUTE WATERPLANE GEOMETRY (LOCAL SPACE)
        // ─────────────────────────────────────────────
        waterplaneEstimator.Compute(
            vesselRoot,
            samplePoints,
            pointHeights,
            sliceIndices,
            sliceCount,
            minZ,
            maxZ
        );

        if (autoComputeStrength)
            RecomputeBuoyancyStrength();
    }

    private void FixedUpdate()
    {
        if (samplePoints == null || pointValid == null)
            return;

        cobSumLocal = Vector3.zero;
        totalBuoyancyForce = 0f;
        totalSubmergedVolume = 0f;

        ApplyAllBuoyancyForces();
        ApplyGlobalHeaveDamping();
        UpdateSternSubmersion();

        if (debugBuoyancy)
        {
            float weight = rb.mass * Physics.gravity.magnitude;
            float diff = totalBuoyancyForce - weight;
            Debug.Log($"[Buoyancy Debug] totalBuoyancyForce={totalBuoyancyForce:F2}, weight={weight:F2}, diff={diff:F2}");
        }

        if (boatCOB != null)
        {
            if (totalBuoyancyForce > 0f)
            {
                Vector3 localCOB = cobSumLocal / totalBuoyancyForce;
                boatCOB.SetLocalCOB(localCOB);
            }

            boatCOB.SetTotalBuoyancyForce(totalBuoyancyForce);
            boatCOB.SetSubmergedVolume(totalSubmergedVolume);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // BUOYANCY LOGIC
    // ─────────────────────────────────────────────────────────────
    private void ApplyBuoyancyAtPoint(int index)
    {
        if (!pointValid[index])
            return;

        ProbeType type = probeTypes[index];
        if (type == ProbeType.Deck)
            return;

        Transform p = samplePoints[index];
        float waterY = pointHeights[index];
        Vector3 normal = pointNormals[index];

        float depth = waterY - p.position.y;

        if (depth > 0f)
        {
            float baseMagnitude = depth * buoyancyStrength * buoyancyScale;
            Vector3 force;
            float effectiveMagnitude;

            switch (type)
            {
                case ProbeType.Keel:
                    force = Vector3.up * (baseMagnitude * keelBuoyancyScale);
                    effectiveMagnitude = baseMagnitude * keelBuoyancyScale;
                    break;

                case ProbeType.Side:
                    force = normal * (baseMagnitude * sideBuoyancyScale);
                    effectiveMagnitude = baseMagnitude * sideBuoyancyScale;
                    break;

                default:
                    return;
            }

            int slice = sliceIndices[index];
            float beam = waterplaneEstimator.sliceBeam[slice];
            float beamFactor = Mathf.Max(beam, 0.01f);

            force /= beamFactor;
            effectiveMagnitude /= beamFactor;

            rb.AddForceAtPosition(force, p.position, ForceMode.Force);
            totalBuoyancyForce += effectiveMagnitude;

            float volume = effectiveMagnitude / (waterDensity.ValueKgPerCubicMeter * Physics.gravity.magnitude);
            totalSubmergedVolume += volume;

            Vector3 localPos = transform.InverseTransformPoint(p.position);
            cobSumLocal += localPos * effectiveMagnitude;

            float verticalVel = Vector3.Dot(rb.GetPointVelocity(p.position), Vector3.up);
            Vector3 damping = -verticalVel * Vector3.up * waterDrag;
            rb.AddForceAtPosition(damping, p.position, ForceMode.Force);

            rb.AddTorque(-rb.angularVelocity * waterAngularDrag, ForceMode.Force);

            if (enableRightingMoment)
            {
                Vector3 tilt = Vector3.Cross(transform.up, normal);

                switch (type)
                {
                    case ProbeType.Keel:
                        rb.AddTorque(tilt * (rightingStrength * keelRightingScale), ForceMode.Force);
                        break;

                    case ProbeType.Side:
                        rb.AddTorque(tilt * (rightingStrength * sideRightingScale), ForceMode.Force);
                        break;
                }
            }
        }
    }

    private void ApplyAllBuoyancyForces()
    {
        for (int i = 0; i < samplePoints.Length; i++)
            ApplyBuoyancyAtPoint(i);
    }

    // ─────────────────────────────────────────────────────────────
    // HEAVE DAMPING
    // ─────────────────────────────────────────────────────────────
    private void ApplyGlobalHeaveDamping()
    {
        float verticalVel = Vector3.Dot(rb.linearVelocity, Vector3.up);
        Vector3 heaveDamping = -verticalVel * Vector3.up * heaveDampingStrength;
        rb.AddForce(heaveDamping, ForceMode.Force);
    }

    // ─────────────────────────────────────────────────────────────
    // STERN IMMERSION
    // ─────────────────────────────────────────────────────────────
    private void UpdateSternSubmersion()
    {
        if (!pointValid[sternProbeIndex])
        {
            SternImmersion01 = 0f;
            return;
        }

        float waterY = pointHeights[sternProbeIndex];
        float pointY = samplePoints[sternProbeIndex].position.y;

        float depth = waterY - pointY;
        SternImmersion01 = Mathf.Clamp01(depth / sternReferenceDepth.ValueMeters);
    }

    // ─────────────────────────────────────────────────────────────
    // CONFIG APPLICATION
    // ─────────────────────────────────────────────────────────────
    public void ApplyBuoyancyConfig(BuoyancyConfig cfg)
    {
        buoyancyScale = cfg.buoyancyScale;
        buoyancyStrength = cfg.buoyancyStrength;
        waterDrag = cfg.waterDrag;
        waterAngularDrag = cfg.waterAngularDrag;

        heaveDampingStrength = cfg.heaveDampingStrength;

        waterDensity = cfg.waterDensity;
        probeArea = cfg.probeArea;
        autoComputeStrength = cfg.autoComputeStrength;

        enableRightingMoment = cfg.enableRightingMoment;
        rightingStrength = cfg.rightingStrength;

        sternProbeIndex = cfg.sternProbeIndex;
        sternReferenceDepth = cfg.sternReferenceDepth;

        if (autoComputeStrength)
            RecomputeBuoyancyStrength();
    }

    // ─────────────────────────────────────────────────────────────
    // AUTO‑COMPUTE BUOYANCY STRENGTH
    // ─────────────────────────────────────────────────────────────
    public void RecomputeBuoyancyStrength()
    {
        if (samplePoints == null || samplePoints.Length == 0)
        {
            buoyancyStrength = 0f;
            return;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            buoyancyStrength = 0f;
            return;
        }

        float g = Physics.gravity.magnitude;
        float targetWeight = rb.mass * g;

        float sumDepthWeighted = 0f;

        for (int i = 0; i < samplePoints.Length; i++)
        {
            ProbeType type = probeTypes[i];
            if (type == ProbeType.Deck)
                continue;

            float waterY = pointHeights[i];
            float depth = waterY - samplePoints[i].position.y;
            if (depth <= 0f)
                continue;

            float typeScale =
                (type == ProbeType.Keel) ? keelBuoyancyScale :
                (type == ProbeType.Side) ? sideBuoyancyScale : 0f;

            if (typeScale <= 0f)
                continue;

            int slice = sliceIndices[i];
            float beam = waterplaneEstimator.sliceBeam[slice];
            float beamFactor = Mathf.Max(beam, 0.01f);

            sumDepthWeighted += depth * typeScale / beamFactor;
        }

        if (sumDepthWeighted <= 0f)
        {
            buoyancyStrength = 0f;
            return;
        }

        buoyancyStrength = targetWeight / sumDepthWeighted;
    }
}