using UnityEngine;

public class PropWash : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("References")]
    [Tooltip("Rigidbody of the boat. If left empty, the script will search the parent.")]
    [SerializeField] private Rigidbody rb;

    [Tooltip("Main particle system used for prop wash and cavitation bursts.")]
    [SerializeField] private ParticleSystem propWash;


    // ─────────────────────────────────────────────────────────────
    // EMISSION (THROTTLE DRIVEN)
    // ─────────────────────────────────────────────────────────────
    [Header("Emission Tuning")]
    [Tooltip("Emission rate at zero throttle.")]
    [SerializeField] private float idleEmission = 5f;

    [Tooltip("Emission rate at full throttle.")]
    [SerializeField] private float maxEmission = 80f;

    [Tooltip("Multiplier applied when throttle is negative (reverse).")]
    [SerializeField] private float reverseMultiplier = 1.5f;


    // ─────────────────────────────────────────────────────────────
    // LIFETIME (VELOCITY DRIVEN)
    // ─────────────────────────────────────────────────────────────
    [Header("Velocity Influence")]
    [Tooltip("Minimum particle lifetime when stationary.")]
    [SerializeField] private float minLifetime = 0.3f;

    [Tooltip("Maximum particle lifetime at max speed.")]
    [SerializeField] private float maxLifetime = 1.2f;

    [Tooltip("Speed at which lifetime reaches maximum.")]
    [SerializeField] private float maxSpeed = 20f;


    // ─────────────────────────────────────────────────────────────
    // CAVITATION SETTINGS
    // ─────────────────────────────────────────────────────────────
    [Header("Cavitation")]
    [Tooltip("How much throttle must change in one frame to trigger cavitation.")]
    [SerializeField] private float cavitationThrottleThreshold = 0.35f;

    [Tooltip("Depth below which cavitation triggers (0 = surface, 1 = fully submerged).")]
    [SerializeField] private float cavitationDepthThreshold = 0.2f;

    [Tooltip("Throttle value (negative) required to trigger reverse cavitation.")]
    [SerializeField] private float reverseCavitationThreshold = -0.8f;

    [Tooltip("Number of particles spawned during a cavitation burst.")]
    [SerializeField] private short cavitationBurstAmount = 20;

    [Tooltip("Minimum time between cavitation bursts.")]
    [SerializeField] private float cavitationCooldown = 0.25f;


    // ─────────────────────────────────────────────────────────────
    // INTERNAL STATE
    // ─────────────────────────────────────────────────────────────
    private float throttleInput;
    private float lastThrottle;
    private float lastCavitationTime;

    private float propDepth = 1f; // 1 = fully submerged, 0 = out of water

    private ParticleSystem.EmissionModule emission;
    private ParticleSystem.MainModule main;


    // ─────────────────────────────────────────────────────────────
    // INITIALISATION
    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (rb == null)
            rb = GetComponentInParent<Rigidbody>();

        emission = propWash.emission;
        main = propWash.main;
    }


    // ─────────────────────────────────────────────────────────────
    // EXTERNAL INPUTS
    // ─────────────────────────────────────────────────────────────
    public void SetThrottle(float value)
    {
        throttleInput = Mathf.Clamp(value, -1f, 1f);
    }

    public void SetPropDepth(float depth01)
    {
        propDepth = Mathf.Clamp01(depth01);
    }


    // ─────────────────────────────────────────────────────────────
    // UPDATE LOOP
    // ─────────────────────────────────────────────────────────────
    private void Update()
    {
        HandleEmission();
        HandleLifetime();
        HandleCavitation();

        lastThrottle = throttleInput;
    }


    // ─────────────────────────────────────────────────────────────
    // EMISSION = THROTTLE
    // ─────────────────────────────────────────────────────────────
    private void HandleEmission()
    {
        float throttle01 = Mathf.Abs(throttleInput);
        float rate = Mathf.Lerp(idleEmission, maxEmission, throttle01);

        if (throttleInput < 0f)
            rate *= reverseMultiplier;

        emission.rateOverTime = rate;
    }


    // ─────────────────────────────────────────────────────────────
    // LIFETIME = VELOCITY
    // ─────────────────────────────────────────────────────────────
    private void HandleLifetime()
    {
        float speed01 = Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
        float lifetime = Mathf.Lerp(minLifetime, maxLifetime, speed01);
        main.startLifetime = lifetime;
    }


    // ─────────────────────────────────────────────────────────────
    // CAVITATION LOGIC
    // ─────────────────────────────────────────────────────────────
    private void HandleCavitation()
    {
        if (Time.time < lastCavitationTime + cavitationCooldown)
            return;

        bool throttleSpike = Mathf.Abs(throttleInput - lastThrottle) > cavitationThrottleThreshold;
        bool shallowProp = propDepth < cavitationDepthThreshold;
        bool reverseSlam = throttleInput < reverseCavitationThreshold;

        if (throttleSpike || shallowProp || reverseSlam)
        {
            TriggerCavitationBurst();
            lastCavitationTime = Time.time;
        }
    }

    private void TriggerCavitationBurst()
    {
        var burst = new ParticleSystem.Burst(0f, cavitationBurstAmount);
        emission.SetBurst(0, burst);
        propWash.Play();
    }
}