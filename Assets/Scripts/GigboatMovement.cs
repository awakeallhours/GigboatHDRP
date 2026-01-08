using UnityEngine;

public class GigboatMovement : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Buoyancy buoyancy;
    [SerializeField] private PropWash propWash;
    [SerializeField] private GigboatGizmos gizmoDrawer;
    [SerializeField] private Transform cameraTarget;


    // ─────────────────────────────────────────────────────────────
    // THROTTLE (REALISTIC MARINE THROTTLE — CLEANED)
    // ─────────────────────────────────────────────────────────────
    [Header("Throttle")]
    [Tooltip("Rate at which throttle changes in response to input.")]
    [SerializeField] private float throttleChangeRate = 60f;

    [Tooltip("Deadzone around neutral to prevent accidental gear flips.")]
    [SerializeField] private float neutralGate = 2f;

    // Internal smoothed magnitude (0..100)
    private float throttleMagnitude;


    // ─────────────────────────────────────────────────────────────
    // PITCH CONTROL
    // ─────────────────────────────────────────────────────────────
    [Header("Pitch & Trim")]
    [Tooltip("Strength of pitch damping. Higher = more stable, less oscillation.")]
    [SerializeField] private float pitchDampingStrength = 10f;


    // ─────────────────────────────────────────────────────────────
    // RUDDER CONTROL
    // (Next section begins in Part 2)
    // ─────────────────────────────────────────────────────────────
    [Header("Rudder Behaviour")]
    [Tooltip("Exponent applied to rudder input for non-linear steering response.")]
    [SerializeField] private float rudderInputExponent = 2.5f;

    [Tooltip("Maximum rudder authority at low speed.")]
    [SerializeField] private float rudderAuthorityLowSpeed = 2f;

    [Tooltip("Base rudder response rate (speed of rudder movement).")]
    [SerializeField] private float rudderResponseRate = 3f;

    [Tooltip("Speed at which rudder response fades toward minimum.")]
    [SerializeField] private float rudderFadeSpeed = 10f;

    [Tooltip("Minimum rudder response at high speed.")]
    [SerializeField] private float rudderMinResponse = 0.2f;

    [Tooltip("Minimum speed required before rudder has any turning effect.")]
    [SerializeField] private float rudderMinEffectiveSpeed = 1f;


    // ─────────────────────────────────────────────────────────────
    // YAW CONTROL
    // ─────────────────────────────────────────────────────────────
    [Header("Yaw Control")]
    [Tooltip("Maximum yaw rate in degrees per second.")]
    [SerializeField] private float maxYawRateDeg = 12f;

    [Tooltip("How strongly speed dampens turning authority.")]
    [SerializeField] private float yawTurnDampingFactor = 0.4f;


    // ─────────────────────────────────────────────────────────────
    // ROLL CONTROL — LEFT UNTOUCHED
    // ─────────────────────────────────────────────────────────────
    [Header("Roll Behaviour")]
    [SerializeField] private float bankResponseSpeed = 120f;
    [SerializeField] private float maxTurnBankAngleDeg = 15f;
    [SerializeField] private float currentDesiredBankDeg = 0f;
    [SerializeField] private float rollDampingStrength = 6f;
    [SerializeField] public float maxRestoringTorque = 6f;

    [Tooltip("Reduces roll stiffness at low speed to prevent spawn wobble.")]
    [SerializeField] private float lowSpeedRollStiffnessMultiplier = 0.05f;

    [Tooltip("Extra roll damping at low speed.")]
    [SerializeField] private float lowSpeedRollDampingMultiplier = 1.5f;

    [Tooltip("Extra damping proportional to roll angle (deg).")]
    [SerializeField] private float rollAngleDampingCoefficient = 0.5f;

    [Tooltip("Base roll stiffness at zero speed.")]
    [SerializeField] private float rollStiffnessBase = 4f;

    [Tooltip("How much roll stiffness increases with speed.")]
    [SerializeField] private float rollStiffnessSpeedMultiplier = 0.1f;

    [Tooltip("Speed at which roll stiffness reaches full effect.")]
    [SerializeField] private float rollStiffnessFullSpeed = 10f;

    [Tooltip("Strength of rudder‑induced roll torque.")]
    [SerializeField] private float rudderRollTorqueStrength = 40f;

    [Tooltip("Minimum speed before rudder‑induced roll begins to fade in.")]
    [SerializeField] private float rudderRollActivationSpeed = 0.3f;

    [Tooltip("Speed range over which rudder‑induced roll fades from 0 to full.")]
    [SerializeField] private float rudderRollFadeRange = 5f;

    [Tooltip("Angle (deg) at which rudder-induced roll torque fades to zero.")]
    [SerializeField] private float rudderRollMaxEffectAngle = 20f;

    [Tooltip("Maximum allowed roll angular velocity in degrees per second.")]
    [SerializeField] private float maxRollAngularVelocityDeg = 30f;

    [Tooltip("Speed at which full roll stiffness is reached.")]
    [SerializeField] private float naturalRollActivationSpeed = 2f;

    [SerializeField, Tooltip("Speed at which full roll damping is reached.")]
    private float naturalRollDampingActivationSpeed = 2f;


    // ─────────────────────────────────────────────────────────────
    // DEBUG / DIAGNOSTICS
    // ─────────────────────────────────────────────────────────────
    [Header("Debug / Diagnostics")]
    [SerializeField] private bool forceUprightTest = false;
    [SerializeField] private float forceUprightTorqueLimit = 50f;


    // ─────────────────────────────────────────────────────────────
    // RUNTIME PROPERTIES
    // ─────────────────────────────────────────────────────────────
    public float RollDampingStrength => rollDampingStrength;
    public float RollStiffnessBase => rollStiffnessBase;
    public float RollStiffnessSpeedMultiplier => rollStiffnessSpeedMultiplier;
    public float RudderRollTorqueStrength => rudderRollTorqueStrength;
    public float RudderRollActivationSpeed => rudderRollActivationSpeed;

    public float RudderAngle
    {
        get; private set;
    }
    public float YawRateDeg
    {
        get; private set;
    }
    public float ThrottlePercent
    {
        get; private set;
    }
    public float TargetThrottle
    {
        get; private set;
    }
    public Rigidbody RB => rb;

    private float speed;
    private float forwardSpeed;
    private float yawRateCommand;

    public Transform CameraTarget => cameraTarget;
    public float SpeedKnots => speed * 1.943844f;

    // ─────────────────────────────────────────────────────────────
    // UNITY EVENTS
    // ─────────────────────────────────────────────────────────────
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        HandleThrottle();
        HandleRudder();
        HandleYawPhysics();
        HandleRollPhysics();
        HandlePitchPhysics();
        UpdateDebugValues();

        propWash.SetPropDepth(buoyancy.SternSubmerged01);
        propWash.SetThrottle(ThrottlePercent);

        // Left untouched — will be cleaned later
        gizmoDrawer.SetThrustForce(Vector3.zero);
    }

    // ─────────────────────────────────────────────────────────────
    // THROTTLE  (REALISTIC MARINE THROTTLE — CLEANED)
    // ─────────────────────────────────────────────────────────────
    private void HandleThrottle()
    {
        // RAW INPUT: keyboard W/S gives -1..+1 instantly
        float input = Input.GetAxisRaw("Vertical");

        // ─────────────────────────────────────────────────────────
        // STEP 1 — APPLY NEUTRAL GATE
        // Prevents accidental gear flips when tapping keys.
        // ─────────────────────────────────────────────────────────
        if (Mathf.Abs(TargetThrottle) < neutralGate)
        {
            if (Mathf.Abs(input) < 0.1f)
                input = 0f;
        }

        // ─────────────────────────────────────────────────────────
        // STEP 2 — UPDATE TARGET THROTTLE (STEPPER LOGIC)
        // W increases throttle gradually
        // S decreases throttle gradually
        // ─────────────────────────────────────────────────────────
        if (input != 0f)
        {
            TargetThrottle += input * throttleChangeRate * Time.fixedDeltaTime;
            TargetThrottle = Mathf.Clamp(TargetThrottle, -100f, 100f);
        }

        // ─────────────────────────────────────────────────────────
        // STEP 3 — REALISTIC MARINE THROTTLE MODEL
        // Direction = sign of TargetThrottle
        // Magnitude = smoothed absolute throttle
        // ─────────────────────────────────────────────────────────

        // 3A — Determine gear direction instantly
        float targetDirection = Mathf.Sign(TargetThrottle);

        // 3B — Determine requested power level (0..100)
        float targetMagnitude = Mathf.Abs(TargetThrottle);

        // 3C — Smooth the magnitude (engine inertia simulation)
        throttleMagnitude = Mathf.MoveTowards(
            throttleMagnitude,
            targetMagnitude,
            throttleChangeRate * Time.fixedDeltaTime
        );

        // 3D — Final throttle output (-100..+100)
        ThrottlePercent = targetDirection * throttleMagnitude;

        // NOTE:
        // Thrust is now handled entirely by MarinePowertrain.
        // No AddForce or thrustPoint logic remains here.
    }



    // ─────────────────────────────────────────────────────────────
    // RUDDER CONTROL
    // ─────────────────────────────────────────────────────────────
    private void HandleRudder()
    {
        float input = Input.GetAxisRaw("Horizontal");

        // Non-linear response curve
        float commanded = Mathf.Sign(input) * Mathf.Pow(Mathf.Abs(input), rudderInputExponent);

        // Speed-based rudder authority
        float speed = rb.linearVelocity.magnitude;
        float fade = Mathf.InverseLerp(rudderFadeSpeed, rudderMinEffectiveSpeed, speed);
        float authority = Mathf.Lerp(rudderMinResponse, rudderAuthorityLowSpeed, fade);

        // Smooth rudder movement
        RudderAngle = Mathf.MoveTowards(
            RudderAngle,
            commanded * authority,
            rudderResponseRate * Time.fixedDeltaTime
        );
    }

    // ─────────────────────────────────────────────────────────────
    // YAW PHYSICS
    // ─────────────────────────────────────────────────────────────
    private void HandleYawPhysics()
    {
        float speed = rb.linearVelocity.magnitude;

        // Convert rudder angle into yaw command
        float yawCommand = RudderAngle * maxYawRateDeg;

        // Speed-based damping
        float damping = 1f / (1f + speed * yawTurnDampingFactor);
        yawCommand *= damping;

        // Apply yaw torque
        rb.AddTorque(Vector3.up * yawCommand, ForceMode.Acceleration);

        // Debug output
        YawRateDeg = rb.angularVelocity.y * Mathf.Rad2Deg;
    }

    // ─────────────────────────────────────────────────────────────
    // ROLL PHYSICS  (LEFT UNTOUCHED AS REQUESTED)
    // ─────────────────────────────────────────────────────────────
    private void HandleRollPhysics()
    {
        float speed = rb.linearVelocity.magnitude;

        // Natural roll stiffness
        float stiffnessSpeedFactor = Mathf.InverseLerp(0f, rollStiffnessFullSpeed, speed);
        float stiffness = rollStiffnessBase + (rollStiffnessSpeedMultiplier * stiffnessSpeedFactor);

        // Low-speed softening
        if (speed < naturalRollActivationSpeed)
            stiffness *= lowSpeedRollStiffnessMultiplier;

        // Roll angle (deg)
        float rollAngle = transform.localEulerAngles.z;
        if (rollAngle > 180f) rollAngle -= 360f;

        // Restoring torque
        float restoringTorque = -rollAngle * stiffness;

        // Roll damping
        float rollAngularVelDeg = rb.angularVelocity.z * Mathf.Rad2Deg;
        float damping = -rollAngularVelDeg * rollDampingStrength;

        // Low-speed damping boost
        if (speed < naturalRollDampingActivationSpeed)
            damping *= lowSpeedRollDampingMultiplier;

        // Angle-based damping
        damping += -rollAngle * rollAngleDampingCoefficient;

        // Rudder-induced roll
        float rudderRoll = 0f;
        if (speed > rudderRollActivationSpeed)
        {
            float fade = Mathf.InverseLerp(
                rudderRollActivationSpeed,
                rudderRollActivationSpeed + rudderRollFadeRange,
                speed
            );

            float rudderEffect = Mathf.Clamp01(1f - Mathf.Abs(rollAngle) / rudderRollMaxEffectAngle);
            rudderRoll = RudderAngle * rudderRollTorqueStrength * fade * rudderEffect;
        }

        // Combine torques
        float totalTorque = restoringTorque + damping + rudderRoll;

        // Safety clamp
        float maxAngularVelRad = maxRollAngularVelocityDeg * Mathf.Deg2Rad;
        if (Mathf.Abs(rb.angularVelocity.z) < maxAngularVelRad)
            rb.AddTorque(new Vector3(0f, 0f, totalTorque), ForceMode.Acceleration);

        // Debug
        currentDesiredBankDeg = Mathf.MoveTowards(
            currentDesiredBankDeg,
            RudderAngle * maxTurnBankAngleDeg,
            bankResponseSpeed * Time.fixedDeltaTime
        );
    }



    // ─────────────────────────────────────────────────────────────
    // PITCH PHYSICS
    // ─────────────────────────────────────────────────────────────
    private void HandlePitchPhysics()
    {
        float pitchVel = rb.angularVelocity.x;
        float damping = -pitchVel * pitchDampingStrength;

        rb.AddTorque(new Vector3(damping, 0f, 0f), ForceMode.Acceleration);
    }



    // ─────────────────────────────────────────────────────────────
    // DEBUG VALUES
    // ─────────────────────────────────────────────────────────────
    private void UpdateDebugValues()
    {
        speed = rb.linearVelocity.magnitude;
        forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
    }

}


