using UnityEngine;

public class Hydrodynamics : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // CORE REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("Core References")]
    [Tooltip("Assigned automatically if left empty.")]
    [SerializeField] private Rigidbody rb;


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

    public bool IsPlaning =>
        forwardDragForce.magnitude > lateralDragForce.magnitude * 2f &&
        Mathf.Abs(transform.eulerAngles.x) < 8f;


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
        ApplyLateralDrag();
        ApplyForwardDrag();
        ApplyYawHydrodynamics();
        ApplyHullDownforce();
    }


    // ─────────────────────────────────────────────────────────────
    // LATERAL DRAG
    // ─────────────────────────────────────────────────────────────
    private void ApplyLateralDrag()
    {
        lateralDragForce = Vector3.zero;
        lateralSpeed = 0f;

        if (lateralLinearDrag == 0f && lateralQuadraticDrag == 0f)
            return;

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
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
    private void ApplyForwardDrag()
    {
        forwardDragForce = Vector3.zero;
        forwardSpeed = 0f;

        if (forwardLinearDrag == 0f && forwardQuadraticDrag == 0f)
            return;

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
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
    private void ApplyYawHydrodynamics()
    {
        yawRate = 0f;
        yawDampingTorque = 0f;

        if (yawDampingCoefficient == 0f && yawLateralCoupling == 0f)
            return;

        float yawVel = rb.angularVelocity.y;
        yawRate = yawVel;

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        float vLat = localVel.x;

        float baseTorque = -yawVel * yawDampingCoefficient;
        float slipTorque = -yawVel * Mathf.Abs(vLat) * yawLateralCoupling;

        float totalTorque = baseTorque + slipTorque;
        yawDampingTorque = totalTorque;

        rb.AddTorque(Vector3.up * totalTorque, ForceMode.Acceleration);
    }


    // ─────────────────────────────────────────────────────────────
    // HULL DOWNFORCE
    // ─────────────────────────────────────────────────────────────
    private void ApplyHullDownforce()
    {
        hullDownforce = Vector3.zero;

        if (hullDownforceCoefficient <= 0f)
            return;

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        float fwd = Mathf.Max(0f, localVel.z);

        float magnitude =
            hullDownforceCoefficient *
            Mathf.Pow(fwd, hullDownforceSpeedExponent);

        hullDownforce = Vector3.down * magnitude;
        rb.AddForce(hullDownforce, ForceMode.Acceleration);
    }
}