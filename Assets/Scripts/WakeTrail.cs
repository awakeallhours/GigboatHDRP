using UnityEngine;

public class WakeTrail : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("References")]
    [Tooltip("Boat rigidbody used to read speed.")]
    [SerializeField] private Rigidbody rb;

    [Tooltip("Particle system used for wake trails.")]
    [SerializeField] private ParticleSystem wakeParticles;


    // ─────────────────────────────────────────────────────────────
    // WAKE SETTINGS
    // ─────────────────────────────────────────────────────────────
    [Header("Wake Settings")]
    [Tooltip("Emission rate when the boat is idle.")]
    [SerializeField] private float idleEmission = 0f;

    [Tooltip("Maximum emission rate at full speed.")]
    [SerializeField] private float maxEmissionRate = 10f;

    [Tooltip("Speed at which wake reaches maximum intensity.")]
    [SerializeField] private float maxWakeSpeed = 20f;

    [Tooltip("Minimum trail width at low speed.")]
    [SerializeField] private float minWidth = 0.2f;

    [Tooltip("Maximum trail width at high speed.")]
    [SerializeField] private float maxWidth = 1.0f;


    // ─────────────────────────────────────────────────────────────
    // INTERNAL
    // ─────────────────────────────────────────────────────────────
    private ParticleSystem.EmissionModule emission;
    private ParticleSystem.TrailModule trails;

    // Optional: assign a water height provider later
    private System.Func<Vector3, float> getWaterHeight;


    // ─────────────────────────────────────────────────────────────
    // UNITY EVENTS
    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (rb == null)
            rb = GetComponentInParent<Rigidbody>();

        if (wakeParticles == null)
            wakeParticles = GetComponentInChildren<ParticleSystem>();

        emission = wakeParticles.emission;
        trails = wakeParticles.trails;
    }

    private void LateUpdate()
    {
        HandleWake();
        FollowWaterSurface();   // ← simple fix + future hook
    }


    // ─────────────────────────────────────────────────────────────
    // WAKE LOGIC
    // ─────────────────────────────────────────────────────────────
    private void HandleWake()
    {
        if (rb == null)
            return;

        float speed = rb.linearVelocity.magnitude;
        float speed01 = Mathf.Clamp01(speed / maxWakeSpeed);

        // Emission
        emission.rateOverTime = idleEmission + (speed01 * maxEmissionRate);

        // Width
        trails.widthOverTrail = Mathf.Lerp(minWidth, maxWidth, speed01);
    }


    // ─────────────────────────────────────────────────────────────
    // WATER SURFACE FOLLOWING
    // ─────────────────────────────────────────────────────────────
    private void FollowWaterSurface()
    {
        if (wakeParticles == null)
            return;

        Transform t = wakeParticles.transform;
        Vector3 pos = t.position;

        // If a water height provider exists, use it
        if (getWaterHeight != null)
        {
            pos.y = getWaterHeight(pos);
            t.position = pos;
            return;
        }

        // SIMPLE FIX (flat water): snap to boat's waterline
        // This prevents floating wake trails when the boat pitches.
        pos.y = rb.worldCenterOfMass.y - 0.5f;   // tweak offset as needed
        t.position = pos;
    }


    // ─────────────────────────────────────────────────────────────
    // FUTURE API: attach a water height sampler
    // ─────────────────────────────────────────────────────────────
    public void SetWaterHeightProvider(System.Func<Vector3, float> provider)
    {
        getWaterHeight = provider;
    }
}