using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MarinePowertrainController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Hydrodynamics hydrodynamics;
    [SerializeField] private AudioEngineController audioEngine;
    [SerializeField] private MarineProp marineProp;

    // ─────────────────────────────────────────────────────────────
    // ENGINE CONFIGURATION
    // ─────────────────────────────────────────────────────────────
    [Header("Engine Configuration")]
    [SerializeField] private float rpmIdle = 600f;
    [SerializeField] private float rpmMax = 2200f;
    [SerializeField] private float rpmChangeRate = 400f;

    // ─────────────────────────────────────────────────────────────
    // LOAD MODELLING
    // ─────────────────────────────────────────────────────────────
    [Header("Load Modelling")]
    [SerializeField] private float referenceMaxDrag = 4000f;
    [SerializeField] private float accelInfluence = 0.4f;
    [SerializeField] private float loadSmoothing = 5f;

    // ─────────────────────────────────────────────────────────────
    // PROP THRUST MODEL
    // ─────────────────────────────────────────────────────────────
    [Header("Propeller Thrust Model")]
    [SerializeField] private float maxStaticThrust = 3500f;
    [SerializeField] private float maxDynamicThrust = 1800f;
    [SerializeField] private float thrustFadeSpeed = 12f;
    [SerializeField] private float reverseThrustMultiplier = 0.6f;

    [SerializeField] private Transform thrustPoint;

    private float currentThrust;
    public Vector3 PropThrustVector
    {
        get; private set;
    }

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
    // GETTERS
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
        UpdatePropThrust();
        ApplyThrust();      // always on now
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
    // ENGINE RPM MODEL
    // ─────────────────────────────────────────────────────────────
    private void UpdateEngineRPM()
    {
        var movement = GetComponent<GigboatMovement>();
        if (movement == null) return;

        float sign = Mathf.Sign(movement.ThrottlePercent);

        float targetRPM = Mathf.Lerp(rpmIdle, rpmMax, throttle01);

        if (Mathf.Approximately(movement.ThrottlePercent, 0f))
            sign = Mathf.Sign(engineRPMPhysical);

        float targetSignedRPM = targetRPM * sign;

        engineRPMPhysical = Mathf.MoveTowards(
            engineRPMPhysical,
            targetSignedRPM,
            rpmChangeRate * Time.fixedDeltaTime
        );

        float rpmMag = Mathf.Abs(engineRPMPhysical);
        engineRPM01 = Mathf.InverseLerp(rpmIdle, rpmMax, rpmMag);
    }

    // ─────────────────────────────────────────────────────────────
    // ENGINE LOAD MODEL
    // ─────────────────────────────────────────────────────────────
    private void UpdateEngineLoad()
    {
        forwardDragMag = hydrodynamics != null
            ? hydrodynamics.ForwardDragForce.magnitude
            : 0f;

        forwardSpeed = hydrodynamics != null
            ? hydrodynamics.ForwardSpeed
            : transform.InverseTransformDirection(rb.linearVelocity).z;

        Vector3 vel = rb.linearVelocity;
        Vector3 deltaV = (vel - lastVelocity) / Time.fixedDeltaTime;
        float forwardAccel = Vector3.Dot(transform.forward, deltaV);
        estimatedAccel = forwardAccel;

        float dragComponent = referenceMaxDrag > 0.001f
            ? Mathf.Clamp01(forwardDragMag / referenceMaxDrag)
            : 0f;

        float accel01 = Mathf.InverseLerp(-2f, 2f, forwardAccel);
        float accelComponent = Mathf.Clamp01(accel01);

        float rawLoad =
            dragComponent * (1f - accelInfluence) +
            accelComponent * accelInfluence;

        rawLoad *= throttle01;

        engineLoad01 = Mathf.MoveTowards(
            engineLoad01,
            Mathf.Clamp01(rawLoad),
            loadSmoothing * Time.fixedDeltaTime
        );
    }

    // ─────────────────────────────────────────────────────────────
    // PROP THRUST MODEL (FINAL)
    // ─────────────────────────────────────────────────────────────
    private void UpdatePropThrust()
    {
        float direction = Mathf.Sign(engineRPMPhysical);

        float speed01 = Mathf.Clamp01(forwardSpeed / thrustFadeSpeed);
        float thrustAtSpeed = Mathf.Lerp(maxStaticThrust, maxDynamicThrust, speed01);

        float thrust = thrustAtSpeed * engineLoad01;

        if (marineProp != null)
            thrust *= marineProp.PropImmersion01;

        if (marineProp != null && !marineProp.PropIsUnderwater)
            thrust *= 0.1f;

        if (direction < 0f)
            thrust *= reverseThrustMultiplier;

        currentThrust = thrust * direction;
        PropThrustVector = transform.forward * currentThrust;
    }

    // ─────────────────────────────────────────────────────────────
    // APPLY THRUST (ALWAYS ON)
    // ─────────────────────────────────────────────────────────────
    private void ApplyThrust()
    {
        if (thrustPoint == null)
            return;

        rb.AddForceAtPosition(PropThrustVector, thrustPoint.position, ForceMode.Force);
    }

    // ─────────────────────────────────────────────────────────────
    // AUDIO OUTPUT
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