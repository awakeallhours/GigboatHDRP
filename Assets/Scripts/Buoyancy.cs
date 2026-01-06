using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Unity.Mathematics;

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
        "Buoyancy spring constant.\n\n" +
        "Recommended:\n" +
        " • For a 1000 kg boat with 6 probes → 4500–5500\n" +
        " • Scales roughly with (mass / desired submersion depth)\n\n" +
        "Higher = boat floats higher and reacts more stiffly.\n" +
        "Lower = boat sits deeper and feels softer."
    )]
    [SerializeField] private float buoyancyStrength = 10f;

    [Tooltip(
        "Per‑probe vertical damping.\n" +
        "This damps vertical velocity at each buoyancy point.\n\n" +
        "Recommended:\n" +
        " • 1.0–2.0 for small boats\n" +
        " • Increase if the boat jitters at each probe."
    )]
    [SerializeField] private float waterDrag = 1f;

    [Tooltip(
        "Angular damping applied when submerged.\n" +
        "Opposes angular velocity.\n\n" +
        "Recommended:\n" +
        " • 0.5–2.0 depending on hull size."
    )]
    [SerializeField] private float waterAngularDrag = 0.5f;

    // ─────────────────────────────────────────────────────────────
    // HEAVE DAMPING (NEW)
    // ─────────────────────────────────────────────────────────────
    [Header("Heave Damping (Vertical Stabilisation)")]

    [Tooltip(
        "Global vertical damping applied to the hull.\n" +
        "This is the primary control for eliminating bounce and porpoising.\n\n" +
        "Recommended values (based on boat mass):\n" +
        " • 1.5 × mass = light damping\n" +
        " • 2.0 × mass = medium damping\n" +
        " • 2.5 × mass = strong damping\n\n" +
        "Example: For a 1000 kg boat → 1500–2500."
    )]
    [SerializeField] private float heaveDampingStrength = 2000f;   // NEW FIELD (name unchanged)

    // ─────────────────────────────────────────────────────────────
    // STERN SUBMERSION
    // ─────────────────────────────────────────────────────────────
    [Header("Stern Submersion")]
    [SerializeField] private int sternIndex = 0;
    [SerializeField] private float sternMaxDepth = 0.5f;

    public float SternSubmerged01
    {
        get; private set;
    }

    // ─────────────────────────────────────────────────────────────
    // DEBUG (Inspector Only)
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

        if (water == null)
            Debug.LogError("Buoyancy: No WaterSurface found in scene.");
        else
            Debug.Log($"Buoyancy: Auto‑assigned WaterSurface '{water.name}'.");

        pointHeights = new float[samplePoints.Length];
        pointNormals = new Vector3[samplePoints.Length];
        pointValid = new bool[samplePoints.Length];
    }

    private void FixedUpdate()
    {
        if (water == null || rb == null || samplePoints.Length == 0)
            return;

        // Apply buoyancy at each probe
        for (int i = 0; i < samplePoints.Length; i++)
            ApplyBuoyancyAtPoint(i);

        // ─────────────────────────────────────────────────────────
        // GLOBAL HEAVE DAMPING (NEW)
        // Opposes vertical velocity of the entire hull.
        // This is the main stabiliser for bounce/porpoising.
        // ─────────────────────────────────────────────────────────
        {
            float verticalVel = Vector3.Dot(rb.linearVelocity, Vector3.up);
            Vector3 heaveDamping = -verticalVel * Vector3.up * heaveDampingStrength;
            rb.AddForce(heaveDamping, ForceMode.Force);
        }

        UpdateSternSubmersion();
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
        }
    }

    private void UpdateSternSubmersion()
    {
        if (sternIndex < 0 || sternIndex >= samplePoints.Length)
        {
            SternSubmerged01 = 0f;
            return;
        }

        if (!pointValid[sternIndex])
        {
            SternSubmerged01 = 0f;
            return;
        }

        float waterY = pointHeights[sternIndex];
        float pointY = samplePoints[sternIndex].position.y;

        float depth = waterY - pointY;
        SternSubmerged01 = Mathf.Clamp01(depth / sternMaxDepth);
    }
}