using UnityEngine;

public class SternSquat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;

    [Tooltip(
        "Buoyancy probes near the stern.\n" +
        "Their average world position is used as the effective stern point\n" +
        "for applying squat forces."
    )]
    [SerializeField] private Transform[] sternProbes;

    [Header("Squat Settings")]
    [Tooltip(
        "Dimensionless squat coefficient.\n\n" +
        "Controls how strongly speed contributes to stern squat.\n\n" +
        "Typical ranges:\n" +
        " • Flat bottom: 0.10–0.15\n" +
        " • Moderate V: 0.05–0.10\n" +
        " • Deep V:     0.02–0.05\n" +
        " • Catamaran:  0.01–0.03\n\n" +
        "Start with ~0.06 for a moderate V hull."
    )]
    [SerializeField] private float squatCoefficient = 0.06f;

    [Tooltip(
        "Additional multiplier on the computed squat force.\n\n" +
        "1.0 = physically reasonable baseline.\n" +
        "Increase to exaggerate the stern sink,\n" +
        "decrease to make the effect more subtle."
    )]
    [SerializeField] private float squatForceMultiplier = 1.0f;

    [Header("Activation")]
    [Tooltip(
        "Speed (m/s) at which squat starts to become noticeable.\n" +
        "Below this speed the effect is smoothly faded out."
    )]
    [SerializeField] private float minSquatSpeed = 2f;

    [Tooltip(
        "Speed (m/s) at which the squat effect reaches full strength.\n" +
        "Above this speed the effect stays at max."
    )]
    [SerializeField] private float maxSquatSpeed = 12f;

    [Tooltip("Enable or disable the squat model without affecting other systems.")]
    [SerializeField] private bool enableSquat = true;

    [Header("Debug (Read Only)")]
    [SerializeField, Tooltip("Current stern squat depth estimate in meters.")]
    private float currentSquatDepthMeters;

    [SerializeField, Tooltip("Current squat force magnitude applied at the stern, in Newtons.")]
    private float currentSquatForceNewtons;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (sternProbes == null || sternProbes.Length == 0)
            Debug.LogWarning("SternSquatModel: sternProbes is empty. Squat will not be applied.");
    }

    private void FixedUpdate()
    {
        if (!enableSquat || rb == null)
            return;

        if (sternProbes == null || sternProbes.Length == 0)
            return;

        float speed = rb.linearVelocity.magnitude;

        // Fade effect between min and max squat speeds
        float speed01 = Mathf.InverseLerp(minSquatSpeed, maxSquatSpeed, speed);
        if (speed01 <= 0f)
        {
            currentSquatDepthMeters = 0f;
            currentSquatForceNewtons = 0f;
            return;
        }

        // Approximate squat depth (meters): squat ≈ k * V² / g
        float g = Physics.gravity.magnitude;
        float squatDepth = squatCoefficient * (speed * speed) / g;

        // Fade with speed so there is no hard threshold
        squatDepth *= speed01;
        currentSquatDepthMeters = squatDepth;

        // Convert depth into an equivalent additional downward force at the stern.
        // Interpreted as "extra effective weight" pressing the stern down.
        float baseWeight = rb.mass * g;
        float squatForceMagnitude = squatDepth * baseWeight * squatForceMultiplier;

        currentSquatForceNewtons = squatForceMagnitude;

        Vector3 sternCenter = GetAverageSternPosition();
        Vector3 squatForce = Vector3.down * squatForceMagnitude;

        rb.AddForceAtPosition(squatForce, sternCenter, ForceMode.Force);
    }

    private Vector3 GetAverageSternPosition()
    {
        Vector3 sum = Vector3.zero;

        for (int i = 0; i < sternProbes.Length; i++)
        {
            if (sternProbes[i] != null)
                sum += sternProbes[i].position;
        }

        return sum / sternProbes.Length;
    }
}