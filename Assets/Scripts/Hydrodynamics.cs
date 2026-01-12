using UnityEngine;

public class Hydrodynamics : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // CORE REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("Core References")]
    [Tooltip("Assigned automatically if left empty.")]
    [SerializeField] private Rigidbody rb;

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
    // RUDDER HYDRODYNAMICS
    // ─────────────────────────────────────────────────────────────
    [Header("Rudder Hydrodynamics")]
    [Tooltip("World-space position of the rudder pivot where force is applied.")]
    [SerializeField] private Transform rudderPivot;

    [Tooltip("Current physical rudder angle in degrees (set by external controller).")]
    [SerializeField] private float rudderAngleDegrees = 0f;

    [Tooltip("Coefficient controlling how strongly rudder generates side force.")]
    [SerializeField] private float rudderForceCoefficient = 500f;

    // ─────────────────────────────────────────────────────────────
    // YAW DEBUG (READ ONLY)
    // ─────────────────────────────────────────────────────────────
    [Header("Yaw Debug (Read Only)")]
    [SerializeField] private float yawRate;
    [SerializeField] private float yawDampingTorque;

    public float YawRate => yawRate;
    public float YawDampingTorque => yawDampingTorque;



    // ─────────────────────────────────────────────────────────────
    // DEBUG (READ ONLY)
    // ─────────────────────────────────────────────────────────────
    [Header("Lateral Debug (Read Only)")]
    [SerializeField] private float lateralSpeed;
    [SerializeField] private Vector3 lateralDragForce;

    [Header("Forward Drag Debug (Read Only)")]
    [SerializeField] private float forwardSpeed;
    [SerializeField] private Vector3 forwardDragForce;

    [Header("Rudder Debug (Read Only)")]
    [SerializeField] private float rudderSideForceMagnitude;
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

    public Vector3 RelativeVelocity => rb.linearVelocity - waterVelocity;

   


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
        // Boat velocity relative to water (currents later).
        Vector3 relVel = rb.linearVelocity - waterVelocity;

        ApplyLateralDrag(relVel);
        ApplyForwardDrag(relVel);
        ApplyRudderHydrodynamics(relVel);
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
    // RUDDER HYDRODYNAMICS
    // ─────────────────────────────────────────────────────────────
    private void ApplyRudderHydrodynamics(Vector3 relVel)
    {
        if (rudderPivot == null)
            return;

        Vector3 localVel = transform.InverseTransformDirection(relVel);
        float fwd = localVel.z;

        // No flow over rudder → no force.
        if (Mathf.Abs(fwd) < 0.1f)
            return;

        float angleRad = rudderAngleDegrees * Mathf.Deg2Rad;
        float forceMag = fwd * Mathf.Sin(angleRad) * rudderForceCoefficient;

        rudderSideForceMagnitude = forceMag;

        Vector3 localForce = new Vector3(forceMag, 0f, 0f);
        rudderForceWorld = transform.TransformDirection(localForce);

        rb.AddForceAtPosition(rudderForceWorld, rudderPivot.position, ForceMode.Force);
    }
}