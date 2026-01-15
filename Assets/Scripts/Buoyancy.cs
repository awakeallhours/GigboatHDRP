using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Axiom.Physics.Units;
using Axiom.Vessel.Diagnostics;

/// <summary>
/// Hybrid buoyancy system using water probes sampled from WaterProbeSampler.
/// Applies upward forces, heave damping, angular damping, and optional righting moment.
/// Also computes Centre of Buoyancy (COB), total buoyancy force, and submerged volume,
/// and reports these values to BoatCOB for diagnostics.
/// </summary>
[DisallowMultipleComponent]
public sealed class Buoyancy : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private Rigidbody rb;

    [Tooltip("Sampler providing per‑probe water height and normal.")]
    [SerializeField] private WaterProbeSampler sampler;

    [Tooltip("Buoyancy state container (COB, volume, force).")]
    [SerializeField] private BoatCOB boatCOB;


    // ─────────────────────────────────────────────────────────────
    // SETTINGS
    // ─────────────────────────────────────────────────────────────

    [Header("Settings")]
    [Tooltip("Base buoyancy strength per meter of submersion depth.")]
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

    [Tooltip("Effective area represented by each probe (m²).")]
    [SerializeField] private AreaValue probeArea;

    [Tooltip("If enabled, buoyancyStrength is auto‑computed from density × g × area.")]
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

    /// <summary>
    /// 0–1 stern immersion ratio based on reference depth.
    /// </summary>
    public float SternImmersion01
    {
        get; private set;
    }


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

    // Sum of (localPosition * forceMagnitude) across all submerged probes.
    private Vector3 cobSumLocal;

    // Total upward buoyancy force (N) across all probes.
    private float totalBuoyancyForce;

    // Total displaced volume (m³) across all probes.
    private float totalSubmergedVolume;


    // ─────────────────────────────────────────────────────────────
    // PUBLIC ACCESSORS (EXTERNAL API)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Exposes water density for external systems (e.g., Hydrodynamics).
    /// </summary>
    public DensityValue WaterDensity => waterDensity;
    public float BuoyancyStrength => buoyancyStrength;


    // ─────────────────────────────────────────────────────────────
    // UNITY EVENTS
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (boatCOB == null)
            boatCOB = GetComponent<BoatCOB>();
    }

    private void Start()
    {
        if (autoComputeStrength)
            RecomputeBuoyancyStrength();
    }

    private void FixedUpdate()
    {
        if (sampler == null || rb == null)
            return;

        // Pull probe data from sampler
        sampler.GetProbeData(out pointValid, out pointHeights, out pointNormals, out samplePoints);

        // Reset accumulators for this frame
        cobSumLocal = Vector3.zero;
        totalBuoyancyForce = 0f;
        totalSubmergedVolume = 0f;

        RunProbeSanityChecks();
        ApplyAllBuoyancyForces();
        ApplyGlobalHeaveDamping();
        UpdateSternSubmersion();

        // Push buoyancy state into BoatCOB for diagnostics
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

    /// <summary>
    /// Applies buoyancy, damping, and optional righting moment at a single probe.
    /// Also accumulates force and volume contributions for COB and diagnostics.
    /// </summary>
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
            // Upward buoyancy force at this probe
            float forceMagnitude = depth * buoyancyStrength;
            Vector3 force = Vector3.up * forceMagnitude;
            rb.AddForceAtPosition(force, p.position, ForceMode.Force);

            // Accumulate total buoyancy force (N)
            totalBuoyancyForce += forceMagnitude;

            // Displaced volume for this probe: V = F / (ρ * g)
            float volume = forceMagnitude / (waterDensity.ValueKgPerCubicMeter * Physics.gravity.magnitude);
            totalSubmergedVolume += volume;

            // Accumulate COB in LOCAL space, weighted by force
            Vector3 localPos = transform.InverseTransformPoint(p.position);
            cobSumLocal += localPos * forceMagnitude;

            // Per‑probe vertical damping
            float verticalVel = Vector3.Dot(rb.GetPointVelocity(p.position), Vector3.up);
            Vector3 damping = -verticalVel * Vector3.up * waterDrag;
            rb.AddForceAtPosition(damping, p.position, ForceMode.Force);

            // Angular damping
            rb.AddTorque(-rb.angularVelocity * waterAngularDrag, ForceMode.Force);

            // Optional righting moment based on water normal
            if (enableRightingMoment)
            {
                Vector3 tilt = Vector3.Cross(transform.up, normal);
                rb.AddTorque(tilt * rightingStrength, ForceMode.Force);
            }
        }
    }

    /// <summary>
    /// Applies buoyancy at all probes.
    /// </summary>
    private void ApplyAllBuoyancyForces()
    {
        for (int i = 0; i < samplePoints.Length; i++)
            ApplyBuoyancyAtPoint(i);
    }


    // ─────────────────────────────────────────────────────────────
    // SANITY CHECKS
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates probe positions and velocities to catch NaNs and insane values early.
    /// </summary>
    private void RunProbeSanityChecks()
    {
        for (int i = 0; i < samplePoints.Length; i++)
        {
            Transform p = samplePoints[i];

            if (float.IsNaN(p.position.x) || float.IsNaN(p.position.y) || float.IsNaN(p.position.z))
            {
                Debug.LogError($"Probe {i} has NaN position: {p.position}");
                continue;
            }

            if (p.position.sqrMagnitude > 1_000_000f)
            {
                Debug.LogError($"Probe {i} is in insane position: {p.position}");
                continue;
            }

            Vector3 probeVel = rb.GetPointVelocity(p.position);
            if (float.IsNaN(probeVel.x) || float.IsNaN(probeVel.y) || float.IsNaN(probeVel.z))
            {
                Debug.LogError($"Probe {i} produced NaN velocity at position {p.position}");
                continue;
            }
        }
    }


    // ─────────────────────────────────────────────────────────────
    // HEAVE DAMPING
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies global vertical damping to reduce heave oscillations.
    /// </summary>
    private void ApplyGlobalHeaveDamping()
    {
        float verticalVel = Vector3.Dot(rb.linearVelocity, Vector3.up);
        Vector3 heaveDamping = -verticalVel * Vector3.up * heaveDampingStrength;
        rb.AddForce(heaveDamping, ForceMode.Force);
    }


    // ─────────────────────────────────────────────────────────────
    // STERN IMMERSION
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates stern immersion ratio based on a designated probe and reference depth.
    /// </summary>
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

    /// <summary>
    /// Applies a BuoyancyConfig to this instance.
    /// Keeps behaviour config‑driven and SI‑clean.
    /// </summary>
    public void ApplyBuoyancyConfig(BuoyancyConfig cfg)
    {
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

    /// <summary>
    /// Recomputes buoyancyStrength from density × g × probe area.
    /// </summary>
    public void RecomputeBuoyancyStrength()
    {
        buoyancyStrength =
            waterDensity.ValueKgPerCubicMeter *
            Physics.gravity.magnitude *
            probeArea.ValueSquareMeters;
    }
}