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
    [Tooltip("Rigidbody receiving buoyancy forces.")]
    [SerializeField] private Rigidbody rb;

    [Tooltip("Water surface provider (adapter implementing IWaterSurface).")]
    [SerializeField] private MonoBehaviour waterSurfaceSource;
    private IWaterSurface waterSurface;

    [Tooltip("Buoyancy state container (COB, volume, force).")]
    [SerializeField] private BoatCOB boatCOB;

    [Tooltip("Probe sampler providing probe positions, heights, normals, validity.")]
    [SerializeField] private WaterProbeSampler probeSampler;

    // ─────────────────────────────────────────────────────────────
    // GLOBAL BUOYANCY CONTROL
    // ─────────────────────────────────────────────────────────────
    [Header("Buoyancy Control")]
    [Tooltip("Global multiplier for all buoyancy forces. Used to calibrate draft at DryMass.")]
    [SerializeField] private float buoyancyScale = 1f;

    // ─────────────────────────────────────────────────────────────
    // BASE SETTINGS
    // ─────────────────────────────────────────────────────────────
    [Header("Settings")]
    [Tooltip("Base buoyancy strength per meter of submersion depth (per probe).")]
    [SerializeField] private float buoyancyStrength = 10f;

    [Tooltip("Linear damping applied at each probe when submerged.")]
    [SerializeField] private float waterDrag = 1f;

    [Tooltip("Angular damping applied when submerged.")]
    [SerializeField] private float waterAngularDrag = 0.5f;

    // ─────────────────────────────────────────────────────────────
    // HEAVE DAMPING
    // ─────────────────────────────────────────────────────────────
    [Header("Heave Damping")]
    [Tooltip("Global vertical damping applied to the rigidbody.")]
    [SerializeField] private float heaveDampingStrength = 2000f;

    // ─────────────────────────────────────────────────────────────
    // HYBRID BUOYANCY (SI‑CLEAN)
    // ─────────────────────────────────────────────────────────────
    [Header("Hybrid Buoyancy (SI Clean)")]
    [Tooltip("Water density (kg/m³).")]
    [SerializeField] private DensityValue waterDensity;

    [Tooltip("Effective area represented by each probe (m²). Used for righting and GM/GZ shaping.")]
    [SerializeField] private AreaValue probeArea;

    [Tooltip("If enabled, buoyancyStrength = density × g × probeArea.")]
    [SerializeField] private bool autoComputeStrength = true;

    [Tooltip("Enable additional righting torque based on water normal.")]
    [SerializeField] private bool enableRightingMoment = true;

    [Tooltip("Scaling factor for righting torque.")]
    [SerializeField] private float rightingStrength = 0.5f;

    // ─────────────────────────────────────────────────────────────
    // STERN IMMERSION SIGNAL
    // ─────────────────────────────────────────────────────────────
    [Header("Stern Immersion")]
    [Tooltip("Index of the probe used to measure stern immersion.")]
    [SerializeField] private int sternProbeIndex = 0;

    [Tooltip("Reference depth for full stern immersion (m).")]
    [SerializeField] private DistanceValue sternReferenceDepth;

    public float SternImmersion01
    {
        get; private set;
    }

    // ─────────────────────────────────────────────────────────────
    // PUBLIC ACCESSORS (EXTERNAL API)
    // ─────────────────────────────────────────────────────────────
    public DensityValue WaterDensity => waterDensity;
    public float BuoyancyStrength => buoyancyStrength;

    // ─────────────────────────────────────────────────────────────
    // INTERNAL PROBE DATA (from sampler)
    // ─────────────────────────────────────────────────────────────
    private bool[] pointValid;
    private float[] pointHeights;
    private Vector3[] pointNormals;
    private Transform[] samplePoints;

    // ─────────────────────────────────────────────────────────────
    // ACCUMULATORS FOR COB + BUOYANCY STATE
    // ─────────────────────────────────────────────────────────────
    private Vector3 cobSumLocal;
    private float totalBuoyancyForce;
    private float totalSubmergedVolume;

    // ─────────────────────────────────────────────────────────────
    // UNITY EVENTS
    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (boatCOB == null)
            boatCOB = GetComponent<BoatCOB>();

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

        if (waterSurface == null)
            Debug.LogError("Buoyancy: No IWaterSurface implementation found in scene.");

        // Auto‑assign sampler if not set
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

        // Pull probe data from sampler (arrays allocated in sampler.Awake)
        probeSampler.GetProbeData(
            out pointValid,
            out pointHeights,
            out pointNormals,
            out samplePoints
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

        Transform p = samplePoints[index];
        float waterY = pointHeights[index];
        Vector3 normal = pointNormals[index];

        float depth = waterY - p.position.y;

        if (depth > 0f)
        {
            float forceMagnitude = depth * buoyancyStrength * buoyancyScale;
            Vector3 force = Vector3.up * forceMagnitude;
            rb.AddForceAtPosition(force, p.position, ForceMode.Force);

            totalBuoyancyForce += forceMagnitude;

            float volume = forceMagnitude / (waterDensity.ValueKgPerCubicMeter * Physics.gravity.magnitude);
            totalSubmergedVolume += volume;

            Vector3 localPos = transform.InverseTransformPoint(p.position);
            cobSumLocal += localPos * forceMagnitude;

            float verticalVel = Vector3.Dot(rb.GetPointVelocity(p.position), Vector3.up);
            Vector3 damping = -verticalVel * Vector3.up * waterDrag;
            rb.AddForceAtPosition(damping, p.position, ForceMode.Force);

            rb.AddTorque(-rb.angularVelocity * waterAngularDrag, ForceMode.Force);

            if (enableRightingMoment)
            {
                Vector3 tilt = Vector3.Cross(transform.up, normal);
                rb.AddTorque(tilt * rightingStrength, ForceMode.Force);
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
        buoyancyStrength =
            waterDensity.ValueKgPerCubicMeter *
            Physics.gravity.magnitude *
            probeArea.ValueSquareMeters;
    }
}