using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MarinePowertrainController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("References")]
    [Tooltip("Rigidbody of the boat.")]
    [SerializeField] private Rigidbody rb;

    [Tooltip("Hydrodynamics component providing drag and forward speed.")]
    [SerializeField] private Hydrodynamics hydrodynamics;

    [Tooltip("Audio-facing engine controller (normalized RPM/load/throttle).")]
    [SerializeField] private AudioEngineController audioEngine;


    // ─────────────────────────────────────────────────────────────
    // ENGINE CONFIGURATION (PHYSICAL)
    // ─────────────────────────────────────────────────────────────
    [Header("Engine Configuration")]
    [Tooltip("Minimum physical RPM at idle.")]
    [SerializeField] private float rpmIdle = 600f;

    [Tooltip("Maximum physical RPM at full throttle.")]
    [SerializeField] private float rpmMax = 2200f;

    [Tooltip("How quickly RPM can change (engine inertia).")]
    [SerializeField] private float rpmChangeRate = 400f;


    // ─────────────────────────────────────────────────────────────
    // LOAD MODELLING
    // ─────────────────────────────────────────────────────────────
    [Header("Load Modelling")]
    [Tooltip("Forward drag value that represents 'full load' for normalisation.")]
    [SerializeField] private float referenceMaxDrag = 4000f;

    [Tooltip("How much acceleration affects load (0 = drag only, 1 = accel only).")]
    [SerializeField] private float accelInfluence = 0.4f;

    [Tooltip("Smoothing applied to final load value.")]
    [SerializeField] private float loadSmoothing = 5f;


    // ─────────────────────────────────────────────────────────────
    // PROP THRUST MODEL (TEMPORARY)
    // ─────────────────────────────────────────────────────────────
    [Header("Propeller Thrust Model")]
    [Tooltip("Thrust at 0 speed (bollard pull).")]
    [SerializeField] private float maxStaticThrust = 3500f;

    [Tooltip("Thrust at high speed (reduced due to slip + drag).")]
    [SerializeField] private float maxDynamicThrust = 1800f;

    [Tooltip("Speed (m/s) where thrust fades from static → dynamic.")]
    [SerializeField] private float thrustFadeSpeed = 12f;

    [Tooltip("Multiplier applied to thrust when reversing.")]
    [SerializeField] private float reverseThrustMultiplier = 0.6f;

    [SerializeField, Tooltip("Current computed thrust (signed).")]
    private float currentThrust;

    public Vector3 PropThrustVector
    {
        get; private set;
    }


    // ─────────────────────────────────────────────────────────────
    // THRUST APPLICATION (MIGRATION TOGGLE)
    // ─────────────────────────────────────────────────────────────
    [Header("Thrust Application (Migration Toggle)")]
    [Tooltip("If true, thrust is applied here instead of GigboatMovement.")]
    [SerializeField] private bool applyThrust = false;

    [Tooltip("Point on the hull where thrust force is applied.")]
    [SerializeField] private Transform thrustPoint;


    // ─────────────────────────────────────────────────────────────
    // DEBUG (READ ONLY)
    // ─────────────────────────────────────────────────────────────
    [Header("Debug (Read Only)")]
    [SerializeField] private float throttle01;
    [SerializeField] private float engineRPMPhysical;
    [SerializeField] private float engineRPM01;
    [SerializeField] private float engineLoad01;
    [SerializeField] private float forwardDragMag;
    [SerializeField] private float forwardSpeed;
    [SerializeField] private float estimatedAccel;

    private Vector3 lastVelocity;


    // ─────────────────────────────────────────────────────────────
    // GETTERS (OPTIONAL FOR DEBUG UI)
    // ─────────────────────────────────────────────────────────────
    public float EngineRPMPhysical => engineRPMPhysical;
    public float EngineRPM01 => engineRPM01;
    public float EngineLoad01 => engineLoad01;
    public float Throttle01 => throttle01;


    // ─────────────────────────────────────────────────────────────
    // UNITY EVENTS
    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (hydrodynamics == null) hydrodynamics = GetComponent<Hydrodynamics>();
        if (audioEngine == null) audioEngine = GetComponent<AudioEngineController>();

        lastVelocity = rb.linearVelocity;
    }

    private void FixedUpdate()
    {
        UpdateThrottleFromMovement();
        UpdateEngineRPM();
        UpdateEngineLoad();
        UpdatePropThrust();       // TEMPORARY
        ApplyThrustIfEnabled();   // MIGRATION TOGGLE
        PushValuesToAudio();

        lastVelocity = rb.linearVelocity;
    }

    // ─────────────────────────────────────────────────────────────
    // THROTTLE INPUT
    // ─────────────────────────────────────────────────────────────
    private void UpdateThrottleFromMovement()
    {
        var movement = GetComponent<GigboatMovement>();
        if (movement == null) return;

        float throttlePercent = movement.ThrottlePercent;
        throttle01 = Mathf.Abs(throttlePercent) / 100f;
    }


    // ─────────────────────────────────────────────────────────────
    // ENGINE RPM MODEL (PHYSICAL → NORMALIZED)
    // ─────────────────────────────────────────────────────────────
    private void UpdateEngineRPM()
    {
        var movement = GetComponent<GigboatMovement>();
        if (movement == null) return;

        float sign = Mathf.Sign(movement.ThrottlePercent);

        // Target physical RPM based on throttle
        float targetRPM = Mathf.Lerp(rpmIdle, rpmMax, throttle01);

        // If throttle is zero, maintain current direction
        if (Mathf.Approximately(movement.ThrottlePercent, 0f))
            sign = Mathf.Sign(engineRPMPhysical);

        float targetSignedRPM = targetRPM * sign;

        // Smooth physical RPM change (engine inertia)
        engineRPMPhysical = Mathf.MoveTowards(
            engineRPMPhysical,
            targetSignedRPM,
            rpmChangeRate * Time.fixedDeltaTime
        );

        // Convert to normalized 0–1 RPM
        float rpmMag = Mathf.Abs(engineRPMPhysical);
        engineRPM01 = Mathf.InverseLerp(rpmIdle, rpmMax, rpmMag);
    }


    // ─────────────────────────────────────────────────────────────
    // ENGINE LOAD MODEL
    // ─────────────────────────────────────────────────────────────
    private void UpdateEngineLoad()
    {
        // Drag-based load
        forwardDragMag = hydrodynamics != null
            ? hydrodynamics.ForwardDragForce.magnitude
            : 0f;

        // Forward speed
        forwardSpeed = hydrodynamics != null
            ? hydrodynamics.ForwardSpeed
            : transform.InverseTransformDirection(rb.linearVelocity).z;

        // Acceleration estimate
        Vector3 vel = rb.linearVelocity;
        Vector3 deltaV = (vel - lastVelocity) / Time.fixedDeltaTime;
        float forwardAccel = Vector3.Dot(transform.forward, deltaV);
        estimatedAccel = forwardAccel;

        // Normalize drag
        float dragComponent = referenceMaxDrag > 0.001f
            ? Mathf.Clamp01(forwardDragMag / referenceMaxDrag)
            : 0f;

        // Normalize acceleration
        float accel01 = Mathf.InverseLerp(-2f, 2f, forwardAccel);
        float accelComponent = Mathf.Clamp01(accel01);

        // Blend drag + acceleration
        float rawLoad =
            dragComponent * (1f - accelInfluence) +
            accelComponent * accelInfluence;

        // Load only matters when throttle is applied
        rawLoad *= throttle01;

        // Smooth load
        engineLoad01 = Mathf.MoveTowards(
            engineLoad01,
            Mathf.Clamp01(rawLoad),
            loadSmoothing * Time.fixedDeltaTime
        );
    }


    // ─────────────────────────────────────────────────────────────
    // PROP THRUST MODEL (TEMPORARY)
    // ─────────────────────────────────────────────────────────────
    private void UpdatePropThrust()
    {
        // 1. Determine direction (forward or reverse)
        float direction = Mathf.Sign(engineRPMPhysical);

        // 2. Compute speed-based thrust fade
        float speed01 = Mathf.Clamp01(forwardSpeed / thrustFadeSpeed);

        // 3. Base thrust at current speed
        float thrustAtSpeed = Mathf.Lerp(maxStaticThrust, maxDynamicThrust, speed01);

        // 4. Scale by engine load
        float thrust = thrustAtSpeed * engineLoad01;


        // ─────────────────────────────────────────────────────────────
        // PROP DRAG OVERRIDE (when throttle is zero)
        // ─────────────────────────────────────────────────────────────
        if (throttle01 < 0.05f)
        {
            // Tuned drag coefficient for 1000 kg hull
            float dragThrust = -forwardSpeed * 330f;

            // Reduce drag at very low speeds
            if (Mathf.Abs(forwardSpeed) < 0.5f)
                dragThrust *= 0.3f;

            // Safety clamp
            dragThrust = Mathf.Clamp(dragThrust, -4000f, 4000f);

            currentThrust = dragThrust;
            PropThrustVector = transform.forward * currentThrust;
            return;
        }

        // 5. Apply reverse thrust shaping
        if (direction < 0f)
            thrust *= reverseThrustMultiplier;

        // 6. Final thrust output
        currentThrust = thrust * direction;
        PropThrustVector = transform.forward * currentThrust;
    }


    // ─────────────────────────────────────────────────────────────
    // APPLY THRUST (MIGRATION TOGGLE)
    // ─────────────────────────────────────────────────────────────
    private void ApplyThrustIfEnabled()
    {
        if (!applyThrust || thrustPoint == null)
            return;

        rb.AddForceAtPosition(PropThrustVector, thrustPoint.position, ForceMode.Force);
    }


    // ─────────────────────────────────────────────────────────────
    // AUDIO OUTPUT (NEW API)
    // ─────────────────────────────────────────────────────────────
    private void PushValuesToAudio()
    {
        if (audioEngine == null) return;

        audioEngine.SetRPM01(engineRPM01);
        audioEngine.SetLoad01(engineLoad01);
        audioEngine.SetThrottle01(throttle01);
        audioEngine.SetSpeed(rb.linearVelocity.magnitude);
        audioEngine.SetReverse(engineRPMPhysical < 0f);
    }
}