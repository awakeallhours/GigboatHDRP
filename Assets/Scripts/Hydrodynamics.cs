using UnityEngine;

public class Hydrodynamics : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // CORE REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("Core References")]
    [Tooltip("Assigned automatically if left empty.")]
    [SerializeField] private Rigidbody rb;

    // Placeholder for water/current velocity (world-space).
    // Currently unused, but once currents are introduced this will be set externally.
    [Tooltip("World-space velocity of water/current. Currently unused until currents are implemented.")]
    [SerializeField] private Vector3 waterVelocity = Vector3.zero;

    // ─────────────────────────────────────────────────────────────
    // LATERAL DRAG
    // ─────────────────────────────────────────────────────────────
    [Header("Lateral Drag")]
    [Tooltip("Low-speed sideways grip. Reduces drifting at slow/medium speeds.")]
    [SerializeField] private float lateralLinearDrag = 0f;

    [Tooltip("High-speed sideways grip. Strongly reduces drifting at high speed.")]
    [SerializeField] private float lateralQuadraticDrag = 0f;

    // ─────────────────────────────────────────────────────────────
    // FORWARD DRAG
    // ─────────────────────────────────────────────────────────────
    [Header("Forward Drag")]
    [Tooltip("Low-speed forward water resistance.")]
    [SerializeField] private float forwardLinearDrag = 0f;

    [Tooltip("High-speed forward water resistance. Limits top speed.")]
    [SerializeField] private float forwardQuadraticDrag = 0f;

    // ─────────────────────────────────────────────────────────────
    // YAW HYDRODYNAMICS
    // ─────────────────────────────────────────────────────────────
    [Header("Yaw Hydrodynamics")]
    [Tooltip("Base yaw resistance from the hull.")]
    [SerializeField] private float yawDampingCoefficient = 0f;

    [Tooltip("Extra yaw resistance when sliding sideways.")]
    [SerializeField] private float yawLateralCoupling = 0f;

    [Tooltip("Below this forward flow, rudder/hull should not induce turning.")]
    [SerializeField] private float rudderMinFlowSpeed = 0.5f; // m/s

    [Tooltip("Reference forward speed for scaling low-flow yaw damping.")]
    [SerializeField] private float rudderAuthorityRefSpeed = 6f; // m/s

    [Tooltip("Extra yaw damping that engages as forward flow approaches zero.")]
    [SerializeField] private float yawLowFlowDamping = 150f; // Nm per rad/s

    // ─────────────────────────────────────────────────────────────
    // ROLL HYDRODYNAMICS
    // ─────────────────────────────────────────────────────────────
    [Header("Roll Hydrodynamics")]
    [Tooltip("How strongly the hull resists rolling motion (damping).")]
    [SerializeField] private float rollDampingCoefficient = 0f;

    [Tooltip("How strongly lateral slip generates roll torque (lean into turns).")]
    [SerializeField] private float rollCouplingCoefficient = 0f;

    [SerializeField] private float rollStiffnessCoefficient = 0f;
    


    [Header("Roll Debug (Read Only)")]
    [SerializeField] private float rollRate;           // rad/s around local X
    [SerializeField] private float rollDampingTorque;  // Nm
    [SerializeField] private float rollCouplingTorque; // Nm

    // ─────────────────────────────────────────────────────────────
    // HULL DOWNFORCE
    // ─────────────────────────────────────────────────────────────
    [Header("Hull Downforce")]
    [Tooltip("Base strength of downward force applied at speed.")]
    [SerializeField] private float hullDownforceCoefficient = 0.5f;

    [Tooltip("Exponent controlling how sharply downforce grows with speed.")]
    [SerializeField] private float hullDownforceSpeedExponent = 1.5f;

    // ─────────────────────────────────────────────────────────────
    // DEBUG (READ ONLY)
    // ─────────────────────────────────────────────────────────────
    [Header("Debug (Read Only)")]
    [SerializeField] private float lateralSpeed;
    [SerializeField] private Vector3 lateralDragForce;

    [Header("Forward Drag Debug (Read Only)")]
    [SerializeField] private float forwardSpeed;
    [SerializeField] private Vector3 forwardDragForce;

    [Header("Yaw Debug (Read Only)")]
    [SerializeField] private float yawRate;
    [SerializeField] private float yawDampingTorque;

    [Tooltip("Debug: actual downforce vector applied this frame.")]
    [SerializeField] private Vector3 hullDownforce;

    // ─────────────────────────────────────────────────────────────
    // PUBLIC GETTERS
    // ─────────────────────────────────────────────────────────────
    public float LateralSpeed => lateralSpeed;
    public Vector3 LateralDragForce => lateralDragForce;

    public float ForwardSpeed => forwardSpeed;   
    public Vector3 ForwardDragForce => forwardDragForce;

    public float YawRate => yawRate;
    public float YawDampingTorque => yawDampingTorque;

    public Vector3 HullDownforce => hullDownforce; //add if i want downforce viewable or need to use it 

    public Vector3 RelativeVelocity => rb.linearVelocity - waterVelocity; //add if i want relative water velocity viewable when i add currents

    // Private backing calculation
    private bool isPlaning =>
        forwardDragForce.magnitude > lateralDragForce.magnitude * 2f &&
        Mathf.Abs(transform.eulerAngles.x) < 8f;

    // Public accessor for debug/UI
    public bool IsPlaningStatus => isPlaning;


    // ─────────────────────────────────────────────────────────────
    // UNITY EVENTS
    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // Compute relative velocity (boat vs water).
        // Currently waterVelocity = Vector3.zero, so relVel == rb.linearVelocity.
        Vector3 relVel = rb.linearVelocity - waterVelocity;

        ApplyLateralDrag(relVel);
        ApplyForwardDrag(relVel);
        ApplyYawHydrodynamics(relVel);
        ApplyRollHydrodynamics(relVel);
        ApplyHullDownforce(relVel);

        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("Manual roll torque triggered.");
            rb.AddTorque(transform.TransformDirection(Vector3.forward) * 20000f, ForceMode.Impulse);

        }

    }

    // ─────────────────────────────────────────────────────────────
    // LATERAL DRAG
    // ─────────────────────────────────────────────────────────────
    private void ApplyLateralDrag(Vector3 relVel)
    {
        lateralDragForce = Vector3.zero;
        lateralSpeed = 0f;

        if (lateralLinearDrag == 0f && lateralQuadraticDrag == 0f)
            return;

        Vector3 localVel = transform.InverseTransformDirection(relVel);
        float vLat = localVel.x;
        lateralSpeed = vLat;

        if (Mathf.Approximately(vLat, 0f))
            return;

        float absV = Mathf.Abs(vLat);
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

        if (forwardLinearDrag == 0f && forwardQuadraticDrag == 0f)
            return;

        Vector3 localVel = transform.InverseTransformDirection(relVel);
        float vFwd = localVel.z;
        forwardSpeed = vFwd;

        if (Mathf.Approximately(vFwd, 0f))
            return;

        float absV = Mathf.Abs(vFwd);
        float dragMag =
            forwardLinearDrag * absV +
            forwardQuadraticDrag * absV * absV;

        float dragLocalZ = -Mathf.Sign(vFwd) * dragMag;
        Vector3 dragLocal = new Vector3(0f, 0f, dragLocalZ);

        forwardDragForce = transform.TransformDirection(dragLocal);
        rb.AddForce(forwardDragForce, ForceMode.Force);
    }

    // ─────────────────────────────────────────────────────────────
    // YAW HYDRODYNAMICS
    // ─────────────────────────────────────────────────────────────
    private void ApplyYawHydrodynamics(Vector3 relVel)
    {
        yawRate = 0f;
        yawDampingTorque = 0f; // no longer used for AddTorque, but we’ll keep it for debug

        if (yawDampingCoefficient == 0f && yawLateralCoupling == 0f && yawLowFlowDamping == 0f)
            return;

        float yawVel = rb.angularVelocity.y;
        yawRate = yawVel;

        Vector3 localVel = transform.InverseTransformDirection(relVel);
        float vLat = localVel.x;
        float forwardFlow = Mathf.Abs(localVel.z);

        // Base hull damping torque (still applied directly)
        float baseTorque = -yawVel * yawDampingCoefficient;

        // Slip‑induced torque (only if flow is present)
        float slipTorque = forwardFlow < rudderMinFlowSpeed ? 0f : -yawVel * Mathf.Abs(vLat) * yawLateralCoupling;

        // Instead of brute‑force low‑flow torque, adjust angularDrag dynamically
        float lowFlowFactor = 1f - Mathf.Clamp01(forwardFlow / rudderAuthorityRefSpeed); 
        rb.angularDamping = Mathf.Lerp(0.5f, 5f, lowFlowFactor);
        // tune 0.5–5 range: 0.5 = normal cruise drag, 5 = heavy stabiliser at standstill

        // Total torque is now just base + slip
        float totalTorque = baseTorque + slipTorque;
        yawDampingTorque = totalTorque; // for debug readout

        rb.AddTorque(Vector3.up * totalTorque, ForceMode.Acceleration);
    }

    // ─────────────────────────────────────────────────────────────
    // ROLL HYDRODYNAMICS — restore + damping only (corrected to Z axis)
    // ─────────────────────────────────────────────────────────────
    private void ApplyRollHydrodynamics(Vector3 relVel)
    {
        rollRate = 0f;
        rollDampingTorque = 0f;
        rollCouplingTorque = 0f;

        if (rollStiffnessCoefficient == 0f && rollDampingCoefficient == 0f)
            return;

        // World angular velocity → local roll rate (about local Z)
        Vector3 angVel = rb.angularVelocity;
        Vector3 localAngVel = transform.InverseTransformDirection(angVel);
        float rollVel = localAngVel.z;
        rollRate = rollVel;

        // Base roll damping (opposes roll velocity)
        float dampingTorqueLocal = -rollVel * rollDampingCoefficient;
        rollDampingTorque = dampingTorqueLocal;

        // Upright restoring torque (spring back to level about local Z)
        float rollAngleRad = Mathf.Deg2Rad * Mathf.DeltaAngle(0f, transform.eulerAngles.z);
        float restoreTorqueLocal = -rollAngleRad * rollStiffnessCoefficient;

        // Total torque in local space (restore + damping only)
        float totalLocalTorque = dampingTorqueLocal + restoreTorqueLocal;

        // Apply torque about local Z in world space
        Vector3 worldTorque = transform.rotation * new Vector3(0f, 0f, totalLocalTorque);
        rb.AddTorque(worldTorque, ForceMode.Acceleration);
    }


    // ─────────────────────────────────────────────────────────────
    // HULL DOWNFORCE
    // ─────────────────────────────────────────────────────────────
    private void ApplyHullDownforce(Vector3 relVel)
    {
        hullDownforce = Vector3.zero;

        if (hullDownforceCoefficient <= 0f)
            return;

        Vector3 localVel = transform.InverseTransformDirection(relVel);
        float fwd = Mathf.Max(0f, localVel.z);

        float magnitude =
            hullDownforceCoefficient *
            Mathf.Pow(fwd, hullDownforceSpeedExponent);

        hullDownforce = Vector3.down * magnitude;
        rb.AddForce(hullDownforce, ForceMode.Acceleration);
    }
}