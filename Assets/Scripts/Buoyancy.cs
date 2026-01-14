using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Axiom.Physics.Units;

public class Buoyancy : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private WaterProbeSampler sampler;

    // ─────────────────────────────────────────────────────────────
    // SETTINGS
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
    // STERN IMMERSION SIGNAL
    // ─────────────────────────────────────────────────────────────
    [Header("Stern Immersion")]
    [SerializeField] private int sternProbeIndex = 0;
    [SerializeField] private DistanceValue sternReferenceDepth;

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

    public DensityValue WaterDensity => waterDensity;

    // ─────────────────────────────────────────────────────────────
    // UNITY EVENTS
    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        
        
    
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

        RunProbeSanityChecks();
        ApplyAllBuoyancyForces();
        ApplyGlobalHeaveDamping();
        UpdateSternSubmersion();
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
            // Upward buoyancy force
            Vector3 force = Vector3.up * (depth * buoyancyStrength);
            rb.AddForceAtPosition(force, p.position, ForceMode.Force);

            // Per‑probe vertical damping
            float verticalVel = Vector3.Dot(rb.GetPointVelocity(p.position), Vector3.up);
            Vector3 damping = -verticalVel * Vector3.up * waterDrag;
            rb.AddForceAtPosition(damping, p.position, ForceMode.Force);

            // Angular damping
            rb.AddTorque(-rb.angularVelocity * waterAngularDrag, ForceMode.Force);

            // Righting moment
            if (enableRightingMoment)
            {
                Vector3 tilt = Vector3.Cross(transform.up, normal);
                rb.AddTorque(tilt * rightingStrength, ForceMode.Force);
            }
        }
    }

    public void RecomputeBuoyancyStrength()
    {
        buoyancyStrength =
            waterDensity.ValueKgPerCubicMeter *
            Physics.gravity.magnitude *
            probeArea.ValueSquareMeters;
    }


    private void ApplyAllBuoyancyForces()
    {
        for (int i = 0; i < samplePoints.Length; i++)
            ApplyBuoyancyAtPoint(i);
    }

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

    private void ApplyGlobalHeaveDamping()
    {
        float verticalVel = Vector3.Dot(rb.linearVelocity, Vector3.up);
        Vector3 heaveDamping = -verticalVel * Vector3.up * heaveDampingStrength;
        rb.AddForce(heaveDamping, ForceMode.Force);
    }

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
}