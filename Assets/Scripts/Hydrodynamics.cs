using UnityEngine;

/// <summary>
/// Axiom: Hydrodynamic model for a displacement workboat hull.
/// Applies:
/// - Lateral drag (sideways skid resistance)
/// - Forward drag (longitudinal resistance / speed limiting)
/// - Rudder side force and yaw moment
/// - Explicit yaw damping (linear + quadratic) to stabilise rotation
///
/// All scalar coefficients here are currently dimensionless tuning factors
/// and are candidates for future SI-value wrappers in the Axiom refactor.
/// </summary>
public class Hydrodynamics : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // CORE REFERENCES
    // ─────────────────────────────────────────────────────────────

    [Header("Core References")]

    [Tooltip("Rigidbody driven by hydrodynamic forces. " +
             "If left null, this component will auto-assign the local Rigidbody on Awake().")]
    [SerializeField] private Rigidbody rb;

    [Tooltip("World-space velocity of the surrounding water (currents, tides). " +
             "Currently assumed constant and typically left at (0,0,0) until currents are implemented.")]
    [SerializeField] private Vector3 waterVelocity = Vector3.zero;

    [SerializeField] private WaterProbeSampler probeSampler;
    [SerializeField] private Buoyancy buoyancy;


    // ─────────────────────────────────────────────────────────────
    // LATERAL DRAG (SIDEWAYS SKID RESISTANCE)
    // ─────────────────────────────────────────────────────────────

    [Header("Lateral Drag")]

    [Tooltip(
        "Low-speed sideways grip coefficient.\n\n" +
        "Acts on the hull's lateral velocity in boat space (local X).\n" +
        "Linear term: F_lat ≈ -C_lin * v_lat.\n\n" +
        "Use this to reduce gentle sideways drift at low to medium speeds.\n\n" +
        "Axiom note: This is a dimensionless tuning factor that will later " +
        "be wrapped into an SI-consistent lateral drag value (e.g. N·s/m).")]
    
    [SerializeField] private float lateralLinearDrag = 0f;

    [Tooltip(
        "High-speed sideways grip coefficient.\n\n" +
        "Quadratic term: F_lat ≈ -C_quad * v_lat * |v_lat|.\n\n" +
        "Use this to strongly clamp sideways skid at higher speeds " +
        "without over-damping low-speed manoeuvring.\n\n" +
        "Axiom note: Dimensionless for now; will later be refactored into " +
        "an SI-clean quadratic lateral drag value (e.g. N·s²/m²).")]
    
    [SerializeField] private float lateralQuadraticDrag = 0f;


    // ─────────────────────────────────────────────────────────────
    // FORWARD DRAG (LONGITUDINAL RESISTANCE)
    // ─────────────────────────────────────────────────────────────

    [Header("Forward Drag")]

    [Tooltip(
        "Low-speed forward water resistance coefficient.\n\n" +
        "Linear term: F_fwd ≈ -C_lin * v_fwd.\n\n" +
        "Controls gentle resistance at low speed and affects acceleration / deceleration feel.\n\n" +
        "Axiom note: Dimensionless; target is an SI-consistent forward drag " +
        "value (e.g. N·s/m) in a future refactor.")]
    
    [SerializeField] private float forwardLinearDrag = 0f;

    [Tooltip(
        "High-speed forward water resistance coefficient.\n\n" +
        "Quadratic term: F_fwd ≈ -C_quad * v_fwd * |v_fwd|.\n\n" +
        "Dominant at higher speeds and primarily responsible for limiting top speed.\n\n" +
        "Axiom note: Dimensionless for now; will be migrated to a proper SI " +
        "quadratic drag representation (e.g. N·s²/m²).")]
    
    [SerializeField] private float forwardQuadraticDrag = 0f;


    // ─────────────────────────────────────────────────────────────
    // RUDDER HYDRODYNAMICS
    // ─────────────────────────────────────────────────────────────

    [Header("Rudder Hydrodynamics")]

    [Tooltip(
        "World-space position of the rudder pivot where the hydrodynamic side force is applied.\n\n" +
        "This point should lie approximately on the rudder stock / hinge line " +
        "and aft of the hull's centre of mass to generate a clear yaw moment.")]
    
    [SerializeField] private Transform rudderPivot;

    [Tooltip(
        "Current physical rudder angle in degrees, injected by the movement / control system.\n\n" +
        "Positive angles typically represent a starboard (right) deflection when looking forward.")]
    
    [SerializeField] private float rudderAngleDegrees = 0f;

    [Tooltip(
        "Rudder side-force coefficient.\n\n" +
        "Used in the quadratic lift-style model:\n" +
        "F_side ≈ v_fwd² * sin(rudderAngle) * C_rudder.\n\n" +
        "Higher values give a more authoritative rudder and stronger yaw moments.\n\n" +
        "Axiom note: Dimensionless placeholder; in an SI-clean refactor this " +
        "will be decomposed into area, water density, and a lift coefficient " +
        "to make the force model fully physical.")]
    
    [SerializeField] private float rudderForceCoefficient = 500f;


    // ─────────────────────────────────────────────────────────────
    // YAW DAMPING COEFFICIENTS
    // ─────────────────────────────────────────────────────────────

    [Header("Yaw Damping")]

    [Tooltip(
        "Linear yaw damping coefficient.\n\n" +
        "Generates a torque proportional to yaw rate ω:\n" +
        "τ_lin ≈ -C_lin * ω.\n\n" +
        "Controls low-speed yaw stability and helps prevent slow oscillations.\n" +
        "Higher values = more resistance to gentle, low-speed turns.\n\n" +
        "Typical working range for a medium workboat hull: 10–40.\n\n" +
        "Axiom note: Currently a dimensionless tuning factor; will later be " +
        "converted to an SI-consistent yaw damping value (e.g. N·m·s/rad).")]
    
    [SerializeField] private float yawLinearDamping = 20f;

    [Tooltip(
        "Quadratic yaw damping coefficient.\n\n" +
        "Generates a torque proportional to ω * |ω|:\n" +
        "τ_quad ≈ -C_quad * ω * |ω|.\n\n" +
        "Controls high-speed rotational stability and clamps aggressive spins " +
        "when yaw rate is large, especially during hard-over reversals.\n" +
        "Higher values = very strong braking when spinning fast.\n\n" +
        "Typical working range for a medium workboat hull: 30–200, depending " +
        "on rudder authority and hull skid.\n\n" +
        "Axiom note: Dimensionless placeholder; target is an SI-clean " +
        "quadratic yaw damping representation (e.g. N·m·s²/rad²).")]
    
    [SerializeField] private float yawQuadraticDamping = 50f;


    // ─────────────────────────────────────────────────────────────
    // YAW DEBUG (READ ONLY)
    // ─────────────────────────────────────────────────────────────

    [Header("Yaw Debug (Read Only)")]

    [Tooltip("Current yaw rate of the hull in degrees per second (about global up).")]
    [SerializeField] private float yawRate;

    [Tooltip("Net yaw damping torque applied this frame (linear + quadratic), in N·m about global up.")]
    [SerializeField] private float yawDampingTorque;

    [Tooltip("Approximate yaw torque generated by the rudder side force this frame, in N·m about global up.")]
    [SerializeField] private float rudderYawTorqueDebug;

    public float YawRate => yawRate;
    public float YawDampingTorque => yawDampingTorque;
    public float RudderYawTorqueDebug => rudderYawTorqueDebug;

    // ─────────────────────────────────────────────────────────────
    // CROSSFLOW MIGRATION
    // ─────────────────────────────────────────────────────────────


    [Header("Crossflow Migration (Temp)")]
   

    [SerializeField] private float crossflowCoefficient = 1.5f;
    [SerializeField] private float crossflowArea = 0.25f;

    // ─────────────────────────────────────────────────────────────
    // TURN REVERSAL DEBUG
    // ─────────────────────────────────────────────────────────────

    [Header("Turn Reversal Debug (Read Only)")]
    [SerializeField] private float yawAccumulatedDegrees;
    [SerializeField] private float lastRudderSign;


    // ─────────────────────────────────────────────────────────────
    // DEBUG (READ ONLY)
    // ─────────────────────────────────────────────────────────────

    [Header("Lateral Debug (Read Only)")]

    [Tooltip("Instantaneous lateral velocity of the hull in local X (m/s).")]
    [SerializeField] private float lateralSpeed;

    [Tooltip("World-space lateral drag force applied this frame (N).")]
    [SerializeField] private Vector3 lateralDragForce;


    [Header("Forward Drag Debug (Read Only)")]

    [Tooltip("Instantaneous forward velocity of the hull in local Z (m/s).")]
    [SerializeField] private float forwardSpeed;

    [Tooltip("World-space forward drag force applied this frame (N).")]
    [SerializeField] private Vector3 forwardDragForce;


    [Header("Rudder Debug (Read Only)")]

    [Tooltip("Scalar side-force magnitude generated by the rudder lift model this frame (N).")]
    [SerializeField] private float rudderSideForceMagnitude;

    [Tooltip("World-space rudder force vector applied at the rudder pivot this frame (N).")]
    [SerializeField] private Vector3 rudderForceWorld;


    // ─────────────────────────────────────────────────────────────
    // PUBLIC GETTERS
    // ─────────────────────────────────────────────────────────────

    public float LateralSpeed => lateralSpeed;
    public Vector3 LateralDragForce => lateralDragForce;

    public float ForwardSpeed => forwardSpeed;
    public Vector3 ForwardDragForce => forwardDragForce;

    public float RudderAngleDegrees
    {
        get => rudderAngleDegrees;
        set => rudderAngleDegrees = value;
    }

    /// <summary>
    /// Boat velocity relative to the surrounding water, in world space (m/s).
    /// </summary>
    public Vector3 RelativeVelocity => rb.linearVelocity - waterVelocity;


    // ─────────────────────────────────────────────────────────────
    // UNITY EVENTS
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Auto-assign Rigidbody if not explicitly wired.
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // Boat velocity relative to water (currents to be implemented later).
        Vector3 relVel = rb.linearVelocity - waterVelocity;

        ApplyLateralDrag(relVel);
        ApplyForwardDrag(relVel);
        ApplyRudderHydrodynamics(relVel);
        ApplyYawDamping();
        ApplyCrossflowDrag();
        UpdateTurnReversalDebug();
    }


    // ─────────────────────────────────────────────────────────────
    // LATERAL DRAG
    // ─────────────────────────────────────────────────────────────

    private void ApplyLateralDrag(Vector3 relVel)
    {
        lateralDragForce = Vector3.zero;
        lateralSpeed = 0f;

        // Early-out if both coefficients are zero.
        if (Mathf.Approximately(lateralLinearDrag, 0f) &&
            Mathf.Approximately(lateralQuadraticDrag, 0f))
            return;

        // Convert to local space to isolate lateral (sideways) component.
        Vector3 localVel = transform.InverseTransformDirection(relVel);
        float vLat = localVel.x;
        lateralSpeed = vLat;

        if (Mathf.Approximately(vLat, 0f))
            return;

        float absV = Mathf.Abs(vLat);

        // Linear + quadratic lateral drag model.
        float dragMag =
            lateralLinearDrag * absV +
            lateralQuadraticDrag * absV * absV;

        float dragLocalX = -Mathf.Sign(vLat) * dragMag;
        Vector3 dragLocal = new Vector3(dragLocalX, 0f, 0f);

        lateralDragForce = transform.TransformDirection(dragLocal);
        rb.AddForce(lateralDragForce, ForceMode.Force);
    }


    // ─────────────────────────────────────────────────────────────
    // FORWARD DRAG
    // ─────────────────────────────────────────────────────────────

    private void ApplyForwardDrag(Vector3 relVel)
    {
        forwardDragForce = Vector3.zero;
        forwardSpeed = 0f;

        // Early-out if both coefficients are zero.
        if (Mathf.Approximately(forwardLinearDrag, 0f) &&
            Mathf.Approximately(forwardQuadraticDrag, 0f))
            return;

        // Convert to local space to isolate forward component.
        Vector3 localVel = transform.InverseTransformDirection(relVel);
        float vFwd = localVel.z;
        forwardSpeed = vFwd;

        if (Mathf.Approximately(vFwd, 0f))
            return;

        float absV = Mathf.Abs(vFwd);

        // Linear + quadratic forward drag model.
        float dragMag =
            forwardLinearDrag * absV +
            forwardQuadraticDrag * absV * absV;

        float dragLocalZ = -Mathf.Sign(vFwd) * dragMag;
        Vector3 dragLocal = new Vector3(0f, 0f, dragLocalZ);

        forwardDragForce = transform.TransformDirection(dragLocal);
        rb.AddForce(forwardDragForce, ForceMode.Force);
    }


    // ─────────────────────────────────────────────────────────────
    // RUDDER HYDRODYNAMICS
    // ─────────────────────────────────────────────────────────────

    private void ApplyRudderHydrodynamics(Vector3 relVel)
    {
        rudderForceWorld = Vector3.zero;
        rudderSideForceMagnitude = 0f;
        rudderYawTorqueDebug = 0f;

        if (rudderPivot == null)
            return;

        // Local forward velocity (flow over the rudder).
        Vector3 localVel = transform.InverseTransformDirection(relVel);
        float vFwd = localVel.z;

        // No meaningful flow over the rudder → no lift.
        if (Mathf.Abs(vFwd) < 0.1f)
            return;

        float angleRad = rudderAngleDegrees * Mathf.Deg2Rad;
        float lift = Mathf.Sin(angleRad);

        // Quadratic rudder lift-style side-force model:
        // F_side ≈ v_fwd² * sin(angle) * C_rudder
        float forceMag = vFwd * Mathf.Abs(vFwd) * lift * rudderForceCoefficient;
        rudderSideForceMagnitude = forceMag;

        // Side force acts in local +X (to starboard) for positive magnitude.
        Vector3 localForce = new Vector3(forceMag, 0f, 0f);
        rudderForceWorld = transform.TransformDirection(localForce);

        // Apply at rudder pivot to generate a yaw moment.
        rb.AddForceAtPosition(rudderForceWorld, rudderPivot.position, ForceMode.Force);

        // Approximate yaw torque from rudder about global up.
        Vector3 r = rudderPivot.position - rb.worldCenterOfMass;
        Vector3 rudderTorque = Vector3.Cross(r, rudderForceWorld);
        float rudderYawTorque = Vector3.Dot(rudderTorque, Vector3.up);

        rudderYawTorqueDebug = rudderYawTorque;
    }


    // ─────────────────────────────────────────────────────────────
    // YAW DAMPING
    // ─────────────────────────────────────────────────────────────

    private void ApplyYawDamping()
    {
        // World-space angular velocity of the hull.
        Vector3 angVel = rb.angularVelocity;

        // Yaw rate around global up (rad/s).
        float yawRateRad = Vector3.Dot(angVel, Vector3.up);

        // For debug / UI: convert to degrees per second.
        yawRate = yawRateRad * Mathf.Rad2Deg;

        float w = yawRateRad;

        // Linear yaw damping term (dominates at low yaw rates).
        float linear = -w * yawLinearDamping;

        // Quadratic yaw damping term (dominates at high yaw rates).
        float quadratic = -w * Mathf.Abs(w) * yawQuadraticDamping;

        float dampingTorqueY = linear + quadratic;
        yawDampingTorque = dampingTorqueY;

        // Apply yaw damping torque about global up.
        rb.AddTorque(new Vector3(0f, dampingTorqueY, 0f), ForceMode.Force);

        // Optional debug: compare damping vs rudder torque visually.
        //Debug.Log($"YAW DEBUG | w={w:F2} rad/s | lin={linear:F1} | quad={quadratic:F1} | " + $"dampTotal={dampingTorqueY:F1} | rudderYaw={rudderYawTorqueDebug:F1}");
    }


    private void UpdateTurnReversalDebug()
    {
        float currentSign = Mathf.Sign(rudderAngleDegrees);

        // Detect rudder sign change (left → right or right → left)
        if (currentSign != 0f && currentSign != lastRudderSign)
        {
            yawAccumulatedDegrees = 0f;
            lastRudderSign = currentSign;
        }

        // Integrate yaw rotation using angular velocity (no wrap issues)
        float yawRateRad = Vector3.Dot(rb.angularVelocity, Vector3.up);
        yawAccumulatedDegrees += yawRateRad * Mathf.Rad2Deg * Time.fixedDeltaTime;

        // Optional debug
        // Debug.Log($"ΔYaw since reversal = {yawAccumulatedDegrees:F1}°");
    }

    private void ApplyCrossflowDrag()
    {
        if (probeSampler == null)
            return;

        if (crossflowCoefficient <= 0f)
            return;

        // Pure horizontal forward direction (boat heading)
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.0001f)
            return;

        // Pure horizontal right direction
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        // Hull lateral velocity in world space (NO rotational component)
        Vector3 velWorld = rb.linearVelocity; // same as in Buoyancy
        float vLat = Vector3.Dot(velWorld, right);

        if (Mathf.Abs(vLat) < 0.1f)
            return;

        probeSampler.GetProbeData(out bool[] valid, out float[] heights, out Vector3[] normals, out Transform[] points);

        float totalDepth = 0f;
        int validCount = 0;

        for (int i = 0; i < points.Length; i++)
        {
            if (!valid[i])
                continue;

            Transform p = points[i];

            float waterY = heights[i];
            float depth = waterY - p.position.y; // > 0 means submerged

            if (depth <= 0f)
                continue;

            totalDepth += depth;
            validCount++;
        }

        if (validCount == 0)
            return;

        float avgDepth = totalDepth / validCount;

        float referenceDepth = 1.0f;
        float submergedFactor = Mathf.Clamp01(avgDepth / referenceDepth);

        if (submergedFactor <= 0f)
            return;

        float rho = buoyancy.WaterDensity.ValueKgPerCubicMeter;
        float A = crossflowArea;
        float Cd = crossflowCoefficient;

        float vLatAbs = Mathf.Abs(vLat);

        float dragMag =
            0.5f *
            rho *
            A *
            Cd *
            vLatAbs * vLatAbs *
            submergedFactor;

        Vector3 dragWorld = -Mathf.Sign(vLat) * dragMag * right;

        Debug.Log($"XF_HYDRO | vLat={vLat:F2} | dragMag={dragMag:F1} | submerged={submergedFactor:F2}");

        rb.AddForce(dragWorld, ForceMode.Force);
    }
}