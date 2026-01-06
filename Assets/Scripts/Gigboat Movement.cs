using Unity.Burst.CompilerServices;
using UnityEngine;
using static Unity.Burst.Intrinsics.Arm;
using UnityEngine.UIElements;
using System.Collections;

public class GigboatMovement : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // CORE REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("Core References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform thrustPoint;
    [SerializeField] public Transform cameraTarget;
    [SerializeField] private Buoyancy buoyancy;

    // ─────────────────────────────────────────────────────────────
    // THROTTLE & PROPULSION
    // ─────────────────────────────────────────────────────────────
    [Header("Throttle & Propulsion")]
    [Tooltip("Maximum forward force applied at full throttle.")]
    [SerializeField] private float maxThrottleForce = 200f;

    [Tooltip("Rate at which throttle changes in response to input.")]
    [SerializeField] private float throttleChangeRate = 60f;

    [SerializeField] private PropWash propWash;

    [SerializeField] private bool usePowertrainThrust = false;

    // ─────────────────────────────────────────────────────────────
    // REALISTIC MARINE THROTTLE (ADDED)
    // ─────────────────────────────────────────────────────────────
    // MIGRATION NOTE:
    // This replaces the old "smooth slider" throttle with a realistic
    // step-based marine throttle:
    // - W increases throttle gradually
    // - S decreases throttle gradually
    // - Direction only changes when crossing zero
    // - No snapping between full forward/reverse
    // - Full subtle control
    // - Keyboard-friendly
    //
    // TargetThrottle remains because keyboard control requires it.
    // ThrottlePercent is now derived using realistic marine logic.

    [Tooltip("Deadzone around neutral to prevent accidental gear flips.")]
    [SerializeField] private float neutralGate = 2f; // percent deadzone

    // Internal smoothed magnitude (0..100)
    private float throttleMagnitude; // ADDED

    // ─────────────────────────────────────────────────────────────
    // PITCH CONTROL
    // ─────────────────────────────────────────────────────────────
    [Header("Pitch & Trim")]
    [Tooltip("Strength of pitch damping. Higher = more stable, less oscillation.")]
    [SerializeField] private float pitchDampingStrength = 10f;

    // unused due to correct physics now
    /*[Tooltip("Downforce coefficient applied to counter bow lift.")]
    [SerializeField] private float pitchDownforceCoefficient = 0.09f;*/

    // ─────────────────────────────────────────────────────────────
    // RUDDER CONTROL
    // ─────────────────────────────────────────────────────────────
    [Header("Rudder Behaviour")]
    [Tooltip("Exponent applied to rudder input for non-linear steering response.")]
    [SerializeField] private float rudderInputExponent = 2.5f;

    // unused for now
    /*[Tooltip("How strongly speed dampens rudder movement.")]
    [SerializeField] private float rudderSpeedDamping = 0.5f;*/

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
    // ROLL CONTROL
    // ─────────────────────────────────────────────────────────────
    [Header("Roll Behaviour")]

    [SerializeField, Tooltip("Max bank angle the hull 'aims' for under lateral acceleration.")]
    private float maxTurnBankAngleDeg = 15f;   

    [Tooltip("Strength of roll damping. Higher = more resistance to roll velocity, reducing wobble.")]
    [SerializeField] private float rollDampingStrength = 6f;

    // ADDED — Low-speed roll stabilisation controls
    [Tooltip("Reduces roll stiffness at low speed to prevent spawn wobble. 1 = normal, <1 = much softer at rest.")]
    [SerializeField] private float lowSpeedRollStiffnessMultiplier = 0.05f;

    [Tooltip("Extra roll damping at low speed. 1 = normal, >1 = stronger damping at rest.")]
    [SerializeField] private float lowSpeedRollDampingMultiplier = 1.5f;

    [Tooltip("Extra damping proportional to roll angle (deg). Helps kill low-speed oscillations without affecting high-speed lean.")]
    [SerializeField] private float rollAngleDampingCoefficient = 0.5f;

    [Tooltip("Base roll stiffness at zero speed. Controls how strongly the boat tries to return upright.")]
    [SerializeField] private float rollStiffnessBase = 4f;

    [Tooltip("How much roll stiffness increases with speed. Higher = stiffer roll at high speed.")]
    [SerializeField] private float rollStiffnessSpeedMultiplier = 0.1f;

    [Tooltip("Speed at which roll stiffness reaches full effect. Controls how quickly the boat transitions from soft to firm roll.")]
    [SerializeField] private float rollStiffnessFullSpeed = 10f;

    [Tooltip("Strength of rudder‑induced roll torque. Controls how much the boat leans into turns.")]
    [SerializeField] private float rudderRollTorqueStrength = 40f;

    [Tooltip("Minimum speed before rudder‑induced roll begins to fade in.")]
    [SerializeField] private float rudderRollActivationSpeed = 0.3f;

    [Tooltip("Speed range over which rudder‑induced roll fades from 0 to full. Larger = smoother onset.")]
    [SerializeField] private float rudderRollFadeRange = 5f;

    [Tooltip("Maximum allowed roll angular velocity in degrees per second. Prevents snapping or violent roll corrections.")]
    [SerializeField] private float maxRollAngularVelocityDeg = 30f;

    [Tooltip("Speed at which full roll stiffness is reached.")]
    [SerializeField] private float naturalRollActivationSpeed = 2f;

    [SerializeField, Tooltip("Speed at which full roll damping is reached.")]
    private float naturalRollDampingActivationSpeed = 2f;

    // ─────────────────────────────────────────────────────────────
    // DEBUG OUTPUT
    // ─────────────────────────────────────────────────────────────
    [Header("Debug Forces (Read Only)")]
    [SerializeField, Tooltip("Net force applied at thrust point.")]
    private Vector3 thrustPointForce;

    [SerializeField, Tooltip("Magnitude of thrust force.")]
    private float thrustPointForceMagnitude;

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
        thrustPointForce = Vector3.zero;

        HandleThrottle();   // MODIFIED in Part B
        HandleRudder();
        HandleYawPhysics();
        HandleRollPhysics();
        HandlePitchPhysics();
        UpdateDebugValues();

        propWash.SetPropDepth(buoyancy.SternSubmerged01);
        propWash.SetThrottle(ThrottlePercent);
    }

    // ─────────────────────────────────────────────────────────────
    // THROTTLE  (REALISTIC MARINE THROTTLE — MODIFIED)
    // ─────────────────────────────────────────────────────────────
    private void HandleThrottle()
    {
        // RAW INPUT: keyboard W/S gives -1..+1 instantly
        float input = Input.GetAxisRaw("Vertical");

        // ─────────────────────────────────────────────────────────
        // STEP 1 — APPLY NEUTRAL GATE (ADDED)
        // Prevents accidental gear flips when tapping keys.
        // This is a small deadzone around 0.
        // ─────────────────────────────────────────────────────────
        if (Mathf.Abs(TargetThrottle) < neutralGate)
        {
            if (Mathf.Abs(input) < 0.1f)
                input = 0f;
        }

        // ─────────────────────────────────────────────────────────
        // STEP 2 — UPDATE TARGET THROTTLE (UNCHANGED BEHAVIOUR)
        // Keyboard acts as a "stepper":
        //   W increases throttle gradually
        //   S decreases throttle gradually
        // This is realistic for digital marine controls.
        // ─────────────────────────────────────────────────────────
        if (input != 0f)
        {
            TargetThrottle += input * throttleChangeRate * Time.fixedDeltaTime;
            TargetThrottle = Mathf.Clamp(TargetThrottle, -100f, 100f);
        }

        // ─────────────────────────────────────────────────────────
        // STEP 3 — REALISTIC MARINE THROTTLE MODEL (ADDED)
        //
        // Direction = sign of TargetThrottle (gear selection)
        // Magnitude = smoothed absolute throttle (engine power)
        //
        // This prevents snapping between full forward/reverse,
        // but still allows smooth, realistic throttle control.
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

        // ─────────────────────────────────────────────────────────
        // STEP 4 — APPLY THRUST (ONLY IF NOT USING POWERTRAIN)
        // ─────────────────────────────────────────────────────────
        if (!usePowertrainThrust)
        {
            float throttle01 = ThrottlePercent / 100f;
            Vector3 force = transform.forward * maxThrottleForce * throttle01;

            rb.AddForceAtPosition(force, thrustPoint.position, ForceMode.Force);

            thrustPointForce += force;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // RUDDER
    // ─────────────────────────────────────────────────────────────
    private void HandleRudder()
    {
        float input = Input.GetAxisRaw("Horizontal");

        speed = rb.linearVelocity.magnitude;
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        forwardSpeed = Mathf.Max(0f, localVel.z);

        float effectiveAuthority = rudderAuthorityLowSpeed / (1f + speed * yawTurnDampingFactor);

        float lowSpeed01 = Mathf.Clamp01((speed - rudderMinEffectiveSpeed) / (4f - rudderMinEffectiveSpeed));
        lowSpeed01 *= lowSpeed01;
        effectiveAuthority *= lowSpeed01;

        float rudderTarget = input;

        if (input != 0f)
        {
            float speed01 = Mathf.Clamp01(speed / rudderFadeSpeed);
            float dampingCurve = Mathf.Lerp(1f, rudderMinResponse, speed01 * speed01);
            float adjustedResponse = rudderResponseRate * dampingCurve;

            RudderAngle = Mathf.MoveTowards(
                RudderAngle,
                rudderTarget,
                adjustedResponse * Time.fixedDeltaTime
            );
        }

        RudderAngle = Mathf.Clamp(RudderAngle, -1f, 1f);

        float curved = Mathf.Sign(RudderAngle) * Mathf.Pow(Mathf.Abs(RudderAngle), rudderInputExponent);
        yawRateCommand = curved * effectiveAuthority;
    }

    // ─────────────────────────────────────────────────────────────
    // YAW PHYSICS
    // ─────────────────────────────────────────────────────────────
    private void HandleYawPhysics()
    {
        if (speed < rudderMinEffectiveSpeed)
            yawRateCommand = 0f;

        rb.AddTorque(Vector3.up * yawRateCommand, ForceMode.Acceleration);

        float maxYawRad = maxYawRateDeg * Mathf.Deg2Rad;
        Vector3 angVel = rb.angularVelocity;
        angVel.y = Mathf.Clamp(angVel.y, -maxYawRad, maxYawRad);
        rb.angularVelocity = angVel;

        YawRateDeg = rb.angularVelocity.y * Mathf.Rad2Deg;
    }


    // ─────────────────────────────────────────────────────────────
    // ROLL PHYSICS — CLEAN, STABLE, NON‑CONTRADICTORY VERSION
    // ─────────────────────────────────────────────────────────────
    private void HandleRollPhysics()
    {
        // --- 1. Compute roll angle ---
        float rollAngle = transform.localEulerAngles.z;
        if (rollAngle > 180f) rollAngle -= 360f;

        // --- 2. Compute roll velocity (angular velocity around forward axis) ---
        float rollVel = Vector3.Dot(rb.angularVelocity, transform.forward);

        // --- 3. Low‑speed roll velocity clamp (prevents spawn wobble) ---
        if (speed < 1f)
        {
            float maxLowSpeedRollVel = 0.5f; // rad/s
            rollVel = Mathf.Clamp(rollVel, -maxLowSpeedRollVel, maxLowSpeedRollVel);
        }

        // --- 4. Roll damping (scaled by speed) ---
        float dampingFade = Mathf.Clamp01(speed / naturalRollDampingActivationSpeed);
        dampingFade *= dampingFade; // smooth fade

        float dampingTorque = rollVel * rollDampingStrength * dampingFade;
        rb.AddTorque(-transform.forward * dampingTorque, ForceMode.Acceleration);

        // --- 5. Natural restoring torque (scaled by speed) ---
        float speed01 = Mathf.Clamp01(speed / rollStiffnessFullSpeed);
        speed01 *= speed01; // smooth curve

        float maxStiffness = rollStiffnessBase + (rollStiffnessSpeedMultiplier * rollStiffnessFullSpeed);

        float stiffnessFade = Mathf.Clamp01(speed / naturalRollActivationSpeed);
        stiffnessFade *= stiffnessFade;

        float stiffness = Mathf.Lerp(rollStiffnessBase, maxStiffness, speed01) * stiffnessFade;

        float restoringTorque = rollAngle * stiffness;
        rb.AddTorque(-transform.forward * restoringTorque, ForceMode.Acceleration);

        // --- 6. Extra angle damping (prevents oscillation around level) ---
        if (Mathf.Abs(rollAngle) > 0.01f)
        {
            float angleDampingTorque = rollAngle * rollAngleDampingCoefficient;
            rb.AddTorque(-transform.forward * angleDampingTorque, ForceMode.Acceleration);
        }

        // --- 7. Angular velocity clamp (safety) ---
        float maxRollVel = maxRollAngularVelocityDeg * Mathf.Deg2Rad;
        Vector3 angVel = rb.angularVelocity;
        angVel.z = Mathf.Clamp(angVel.z, -maxRollVel, maxRollVel);
        rb.angularVelocity = angVel;
    }

    // ─────────────────────────────────────────────────────────────
    // PITCH PHYSICS
    // ─────────────────────────────────────────────────────────────
    private void HandlePitchPhysics()
    {
        float pitchVel = Vector3.Dot(rb.angularVelocity, transform.right);
        rb.AddTorque(-transform.right * (pitchVel * pitchDampingStrength), ForceMode.Acceleration);

        Vector3 angVel = rb.angularVelocity;
        float maxPitchRad = 20f * Mathf.Deg2Rad;
        angVel.x = Mathf.Clamp(angVel.x, -maxPitchRad, maxPitchRad);
        rb.angularVelocity = angVel;
    }

    // ─────────────────────────────────────────────────────────────
    // DEBUG
    // ─────────────────────────────────────────────────────────────
    private void UpdateDebugValues()
    {
        thrustPointForceMagnitude = thrustPointForce.magnitude;
    }

    // ─────────────────────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb == null)
            return;

        // -----------------------------
        // 1. COM VISUALISATION
        // -----------------------------
        Vector3 com = rb.worldCenterOfMass;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(com, 0.15f);

        Gizmos.DrawLine(com + Vector3.up * 2f, com - Vector3.up * 2f);

        float cross = 0.5f;
        Gizmos.DrawLine(com + Vector3.right * cross, com - Vector3.right * cross);
        Gizmos.DrawLine(com + Vector3.forward * cross, com - Vector3.forward * cross);

        // -----------------------------
        // 2. HULL BOTTOM REFERENCE
        // -----------------------------
        float hullBottomLocalY = 0f;
        Vector3 hullBottom = transform.TransformPoint(new Vector3(0f, hullBottomLocalY, 0f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(hullBottom, new Vector3(0.15f, 0.02f, 0.15f));

        Gizmos.DrawLine(hullBottom, com);

        // -----------------------------
        // 3. THRUST POINT VISUALISATION
        // -----------------------------
        if (thrustPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(thrustPoint.position, 0.12f);

            Gizmos.DrawLine(thrustPoint.position + Vector3.up * 1.5f,
                            thrustPoint.position - Vector3.up * 1.5f);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(thrustPoint.position, com);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(thrustPoint.position,
                            thrustPoint.position + thrustPointForce * 0.01f);
        }

        // -----------------------------
        // 4. VELOCITY + SLIP (runtime only)
        // -----------------------------
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(com, com + transform.forward * 3f);

            Vector3 vel = rb.linearVelocity;
            if (vel.sqrMagnitude > 0.01f)
            {
                Vector3 velDir = vel.normalized;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(com, com + velDir * 3f);

                Vector3 localVel = transform.InverseTransformDirection(vel);
                Vector3 lateral = new Vector3(localVel.x, 0f, 0f);
                Vector3 lateralWorld = transform.TransformDirection(lateral);

                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(com, com + lateralWorld * 2f);
            }
        }
    }
}