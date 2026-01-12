using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Unity.Mathematics;
using Axiom.Physics.Units;

public class Buoyancy : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private Rigidbody rb;

    private WaterSurface water;

    // ─────────────────────────────────────────────────────────────
    // BUOYANCY POINTS
    // ─────────────────────────────────────────────────────────────
    [Header("Buoyancy Points")]
    [SerializeField] private Transform[] samplePoints;

    // ─────────────────────────────────────────────────────────────
    // SETTINGS
    // ─────────────────────────────────────────────────────────────
    [Header("Settings")]

    [Tooltip(
    "Spring constant for buoyancy.\n\n" +
    "If Auto Compute is enabled, this value is derived from:\n" +
    "    buoyancyStrength = waterDensity × gravity × probeArea\n\n" +
    "This produces physically realistic buoyancy forces.\n" +
    "If Auto Compute is disabled, this acts as a manual tuning constant."
    )]
    [SerializeField] private float buoyancyStrength = 10f;

    [Tooltip(
    "Vertical damping applied at each buoyancy probe.\n\n" +
    "This reduces jitter and vertical oscillation.\n" +
    "Higher values = stronger damping.\n" +
    "Recommended: 1.0–2.0 for small craft.")]
    [SerializeField] private float waterDrag = 1f;

    [Tooltip(
    "Angular damping applied when the hull is submerged.\n\n" +
    "Opposes angular velocity (roll, pitch, yaw).\n" +
    "Higher values = more stable but less responsive.")]
    [SerializeField] private float waterAngularDrag = 0.5f;

    // ─────────────────────────────────────────────────────────────
    // HEAVE DAMPING
    // ─────────────────────────────────────────────────────────────
    [Header("Heave Damping")]
    [Tooltip(
    "Global vertical damping applied to the hull.\n\n" +
    "This is the primary control for eliminating bounce.\n" +
    "Recommended: 1.5–2.5 × boat mass.")]
    [SerializeField] private float heaveDampingStrength = 2000f;

    // ─────────────────────────────────────────────────────────────
    // HYBRID BUOYANCY (SI‑CLEAN)
    // ─────────────────────────────────────────────────────────────
    [Header("Hybrid Buoyancy (SI Clean)")]
    [Tooltip("Density of the water.\n\n" +
    "Freshwater ≈ 1000 kg/m³\n" +
    "Saltwater ≈ 1025 kg/m³\n\n" +
    "Choose Freshwater or Saltwater for realistic behaviour,\n" +
    "or Custom to enter a specific density.")]
    [SerializeField] private DensityValue waterDensity;
    
    [Tooltip("Effective surface area (m²) represented by each buoyancy probe.\n\n" +
    "Larger boats require larger probe areas.\n" +
    "Typical values:\n" +
    " • Small craft: 0.15–0.35 m²\n" +
    " • Medium craft: 0.35–0.75 m²\n" +
    " • Large hulls: 0.75–2.0 m²")]
    [SerializeField] private AreaValue probeArea;
    
    [SerializeField] private bool autoComputeStrength = true;

    [SerializeField] private bool enableRightingMoment = true;
    
    [Tooltip("Applies a stabilising torque that pushes the hull upright.\n\n" +
    "This approximates real hull stability without requiring\n" +
    "complex mesh‑based buoyancy.\n\n" +
    "Higher values = stronger roll stability.\n" +
    "Recommended: 0.2–0.7 for small craft.")]

    [SerializeField] private float rightingStrength = 0.5f;

    // ─────────────────────────────────────────────────────────────
    // CROSSFLOW DRAG (LATERAL HYDRODYNAMIC DAMPING)
    // ─────────────────────────────────────────────────────────────
    [Header("Crossflow Drag (Hydrodynamic Roll/Skid Damping)")]

    [Tooltip("Coefficient controlling how strongly each submerged probe resists sideways motion. Typical range: 0.5–3.0.")]
    [SerializeField] private float crossflowCoefficient = 1.5f;

    [Tooltip("Effective lateral area per probe used for crossflow drag. Usually matches probe area.")]
    [SerializeField] private float crossflowArea = 0.25f;

    [SerializeField] float lateralDragCoefficient = 2f;   // tune
    [SerializeField] float lateralDragArea = 0.5f;        // tune

    // ─────────────────────────────────────────────────────────────
    // STERN IMMERSION SIGNAL
    // ─────────────────────────────────────────────────────────────
    [Header("Stern Immersion")]
    [Tooltip(
        "Reference depth (meters) used to normalise stern immersion.\n" +
        "SternImmersion = depth / sternReferenceDepth."
    )]
    [SerializeField] private int sternProbeIndex = 0;          //references the selected stern for signals later

    [SerializeField] private DistanceValue sternReferenceDepth;

    public float SternImmersion01
    {
        get; private set;
    }


    // ─────────────────────────────────────────────────────────────
    // DEBUG
    // ─────────────────────────────────────────────────────────────
    [Header("Debug (Read Only)")]
    [SerializeField] private float[] pointHeights;
    [SerializeField] private Vector3[] pointNormals;
    [SerializeField] private bool[] pointValid;

    // ─────────────────────────────────────────────────────────────
    // UNITY EVENTS
    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        water = FindFirstObjectByType<WaterSurface>();

        pointHeights = new float[samplePoints.Length];
        pointNormals = new Vector3[samplePoints.Length];
        pointValid = new bool[samplePoints.Length];

        if (autoComputeStrength)
        {
            buoyancyStrength =
                waterDensity.ValueKgPerCubicMeter *
                Physics.gravity.magnitude *
                probeArea.ValueSquareMeters;

            Debug.Log($"[Buoyancy] Auto‑computed buoyancyStrength = {buoyancyStrength:F2}");
        }
    }

    private void FixedUpdate()
    {
        if (water == null || rb == null)
            return;

        // --- PROBE SANITY CHECKS ---
        for (int i = 0; i < samplePoints.Length; i++)
        {
            Transform p = samplePoints[i];

            // 1. Check for NaN
            if (float.IsNaN(p.position.x) || float.IsNaN(p.position.y) || float.IsNaN(p.position.z))
            {
                Debug.LogError($"Probe {i} has NaN position: {p.position}");
                continue;
            }

            // 2. Check for insane world position (>1000m from origin)
            if (p.position.sqrMagnitude > 1000000f)
            {
                Debug.LogError($"Probe {i} is in insane position: {p.position}");
                continue;
            }

            // 3. Check velocity at probe
            Vector3 probeVel = rb.GetPointVelocity(p.position);
            if (float.IsNaN(probeVel.x) || float.IsNaN(probeVel.y) || float.IsNaN(probeVel.z))
            {
                Debug.LogError($"Probe {i} produced NaN velocity at position {p.position}");
                continue;
            }
        }

        // --- APPLY BUOYANCY ---
        for (int i = 0; i < samplePoints.Length; i++)
            ApplyBuoyancyAtPoint(i);

        // --- GLOBAL HEAVE DAMPING ---
        float verticalVel = Vector3.Dot(rb.linearVelocity, Vector3.up);
        Vector3 heaveDamping = -verticalVel * Vector3.up * heaveDampingStrength;
        rb.AddForce(heaveDamping, ForceMode.Force);

        // --- HYDRODYNAMIC FORCES ---
        ApplyCrossflowDrag();
        //ApplyHullLateralDrag();
        UpdateSternSubmersion();

        // --- DEBUG LOCAL VELOCITY ---
        Vector3 velWorld = rb.linearVelocity;
        Vector3 velLocal = transform.InverseTransformDirection(velWorld);

        if (velWorld.magnitude > 1f)
        {
            Debug.Log($"VEL LOCAL: x={velLocal.x:F2}, y={velLocal.y:F2}, z={velLocal.z:F2}");
        }
    }


    // ─────────────────────────────────────────────────────────────
    // BUOYANCY LOGIC
    // ─────────────────────────────────────────────────────────────
    private void ApplyBuoyancyAtPoint(int index)
    {
        Transform p = samplePoints[index];

        WaterSearchParameters wp = new WaterSearchParameters();
        WaterSearchResult wr;

        wp.startPositionWS = p.position;
        wp.targetPositionWS = p.position;
        wp.error = 0.01f;
        wp.maxIterations = 8;

        bool ok = water.ProjectPointOnWaterSurface(wp, out wr);
        pointValid[index] = ok;

        if (!ok)
            return;

        float waterY = wr.projectedPositionWS.y;
        pointHeights[index] = waterY;

        Vector3 normal = new Vector3(wr.normalWS.x, wr.normalWS.y, wr.normalWS.z);
        pointNormals[index] = normal;

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
                Vector3 up = transform.up;
                Vector3 targetUp = normal;
                Vector3 tilt = Vector3.Cross(up, targetUp);
                rb.AddTorque(tilt * rightingStrength, ForceMode.Force);
            }
        }
    }

    private void ApplyCrossflowDrag()
    {
        if (crossflowCoefficient <= 0f)
            return;

        // Pure horizontal forward direction
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        // Pure horizontal right direction
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        for (int i = 0; i < samplePoints.Length; i++)
        {
            if (!pointValid[i])
                continue;

            Transform p = samplePoints[i];

            float waterY = pointHeights[i];
            float depth = waterY - p.position.y;
            if (depth <= 0f)
                continue;

            // Velocity at probe
            Vector3 velWorld = rb.GetPointVelocity(p.position);

            // Lateral velocity = projection onto horizontal right
            float vLat = Vector3.Dot(velWorld, right);
            if (Mathf.Abs(vLat) < 0.1f)
                continue;

            // Quadratic drag
            float dragMag =
                0.5f *
                waterDensity.ValueKgPerCubicMeter *
                crossflowArea *
                crossflowCoefficient *
                vLat * Mathf.Abs(vLat);

            // Oppose lateral motion, purely horizontal
            Vector3 dragWorld = -Mathf.Sign(vLat) * dragMag * right;

            // Apply at probe
            rb.AddForceAtPosition(dragWorld, p.position, ForceMode.Force);
        }
    }



    /*void ApplyHullLateralDrag()
    {
        // Boat-local velocity at the rigidbody's center
        Vector3 velWorld = rb.linearVelocity;
        Vector3 velLocal = transform.InverseTransformDirection(velWorld);

        // Lateral velocity is local X
        float vLat = velLocal.x;
        if (Mathf.Abs(vLat) < 0.1f)
            return;

        // Quadratic lateral drag
        float dragMag =
            0.5f *
            waterDensity.ValueKgPerCubicMeter *
            lateralDragArea *
            lateralDragCoefficient *
            vLat * Mathf.Abs(vLat);

        // Oppose lateral motion in local space
        float dragLocalX = -Mathf.Sign(vLat) * dragMag;
        Vector3 dragLocal = new Vector3(dragLocalX, 0f, 0f);

        // Back to world, applied at CoM → NO extra pitch/roll torque
        Vector3 dragWorld = transform.TransformDirection(dragLocal);

        rb.AddForce(dragWorld, ForceMode.Force);
    }*/


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

}