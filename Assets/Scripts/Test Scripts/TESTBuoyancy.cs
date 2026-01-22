using UnityEngine;

/// <summary>
/// Physically accurate buoyancy test harness using the new GameObject-based
/// probe system. Implements Archimedes' principle per probe:
///     F = density * g * area * depth
/// No sampler, no normals, no righting, no COB.
/// </summary>
[DisallowMultipleComponent]
public class TESTBuoyancy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private AxiomBuoyancyVessel vessel;

    [Header("Water Settings")]
    [Tooltip("Flat water plane height (Y).")]
    [SerializeField] private float waterHeight = 0f;

    [Header("Physical Parameters")]
    [Tooltip("Water density (kg/m³).")]
    [SerializeField] private float waterDensity = 1025f; // seawater

    [Tooltip("Effective area represented by each probe (m²).")]
    [SerializeField] private float probeArea = 0.1f;

    [Tooltip("Small vertical damping to reduce jitter.")]
    [SerializeField] private float dampingStrength = 1f;

    private float buoyancyStrength;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (vessel == null)
            Debug.LogError("TESTBuoyancy: No vessel assigned.");

        // Compute buoyancy strength exactly like your real system
        buoyancyStrength = waterDensity * Physics.gravity.magnitude * probeArea;

        Debug.Log("Probe count = " + vessel.ProbeObjects.Count);
    }

    private void FixedUpdate()
    {
        if (vessel == null)
            return;

        var probes = vessel.ProbeObjects;
        if (probes == null || probes.Count == 0)
            return;

        foreach (var probe in probes)
        {
            if (probe == null)
                continue;

            Vector3 pos = probe.position;
            float depth = waterHeight - pos.y;

            if (depth > 0f)
            {
                // Archimedes force
                float magnitude = depth * buoyancyStrength;
                Vector3 force = Vector3.up * magnitude;

                rb.AddForceAtPosition(force, pos, ForceMode.Force);

                // Simple vertical damping
                float verticalVel = Vector3.Dot(rb.GetPointVelocity(pos), Vector3.up);
                Vector3 damping = -verticalVel * Vector3.up * dampingStrength;
                rb.AddForceAtPosition(damping, pos, ForceMode.Force);
            }
        }
    }
}