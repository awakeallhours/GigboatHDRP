using UnityEngine;

public class Hydrodynamics : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("This is assigned automatically through code")]
    [SerializeField] private Rigidbody rb;

    [Header("Lateral Drag")]
    [Tooltip("Low-speed sideways grip. Higher values reduce drifting at slow and medium speeds. Think of this as the boat's basic resistance to sliding sideways.")]
    [SerializeField] private float lateralLinearDrag = 0f;
    [Tooltip("High-speed sideways grip. Strongly reduces drifting when the boat is moving fast. Helps the hull 'bite' the water during sharp turns.")]
    [SerializeField] private float lateralQuadraticDrag = 0f;

    [Header("Forward Drag")]
    [Tooltip("Low-speed forward water resistance. Higher values make the boat slow down more quickly when you release the throttle.")]
    [SerializeField] private float forwardLinearDrag = 0f;

    [Tooltip("High-speed forward water resistance. Strongly limits top speed and adds realistic water braking at high velocity.")]
    [SerializeField] private float forwardQuadraticDrag = 0f;

    [Header("Yaw Hydrodynamics")]
    [Tooltip("Base yaw resistance from the hull. Higher values make the boat resist spinning (yaw) more strongly.")]
    [SerializeField] private float yawDampingCoefficient = 0f;

    [Tooltip("Extra yaw resistance when there is sideways (lateral) motion. Helps the stern 'bite' the water in turns.")]
    [SerializeField] private float yawLateralCoupling = 0f;

    [Header("Hull Downforce")]

    [Tooltip("Base strength of the downward force applied to the hull at speed. " +
         "Higher values make the boat feel more planted and reduce high‑speed slide.")]
    [SerializeField] private float hullDownforceCoefficient = 0.5f;

    [Tooltip("How sharply downforce increases with forward speed. " +
             "1 = linear, 1.5–2 = realistic for small planing hulls.")]
    [SerializeField] private float hullDownforceSpeedExponent = 1.5f;

    [Header("Debug (Read Only)")]
    [SerializeField] private float lateralSpeed;
    [SerializeField] private Vector3 lateralDragForce;
    
    [Header("Forward Drag Debug (Read Only)")]
    [SerializeField] private float forwardSpeed;
    [SerializeField] private Vector3 forwardDragForce;

    [Header("Yaw Debug (Read Only)")]
    [SerializeField] private float yawRate;           // rad/s around up axis
    [SerializeField] private float yawDampingTorque;  // scalar for debug

    [Tooltip("Debug: shows the actual downforce vector applied this frame.")]
    [SerializeField] private Vector3 hullDownforce;

    public float LateralSpeed => lateralSpeed;
    public Vector3 LateralDragForce => lateralDragForce;

    public float ForwardSpeed => forwardSpeed;
    public Vector3 ForwardDragForce => forwardDragForce;
    public float YawRate => yawRate;
    public float YawDampingTorque => yawDampingTorque;

    public bool IsPlaning
    {
        get
        {
            // Self-adjusting logic: no thresholds to maintain
            // Boat is planing when hydrodynamic forces dominate buoyancy
            return forwardDragForce.magnitude > lateralDragForce.magnitude * 2f &&
                   Mathf.Abs(transform.eulerAngles.x) < 8f;
        }
    }


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

    private void ApplyLateralDrag()
    {
        // Safe by default: no drag when coefficients are zero
        lateralDragForce = Vector3.zero;
        lateralSpeed = 0f;

        if (lateralLinearDrag == 0f && lateralQuadraticDrag == 0f)
            return;

        Vector3 worldVel = rb.linearVelocity;
        Vector3 localVel = transform.InverseTransformDirection(worldVel);

        float vLat = localVel.x;  // sideways in boat space
        lateralSpeed = vLat;

        if (Mathf.Approximately(vLat, 0f))
            return;

        float sign = Mathf.Sign(vLat);
        float absV = Mathf.Abs(vLat);

        float dragMag =
            lateralLinearDrag * absV +
            lateralQuadraticDrag * absV * absV;

        // Oppose lateral movement
        float dragLocalX = -sign * dragMag;

        Vector3 dragLocal = new Vector3(dragLocalX, 0f, 0f);
        lateralDragForce = transform.TransformDirection(dragLocal);

        rb.AddForce(lateralDragForce, ForceMode.Force);
    }

    private void ApplyForwardDrag()
    {
        forwardDragForce = Vector3.zero;
        forwardSpeed = 0f;

        // No drag if coefficients are zero
        if (forwardLinearDrag == 0f && forwardQuadraticDrag == 0f)
            return;

        Vector3 worldVel = rb.linearVelocity;
        Vector3 localVel = transform.InverseTransformDirection(worldVel);

        float vFwd = localVel.z;  // forward speed in boat space
        forwardSpeed = vFwd;

        if (Mathf.Approximately(vFwd, 0f))
            return;

        float sign = Mathf.Sign(vFwd);
        float absV = Mathf.Abs(vFwd);

        float dragMag =
            forwardLinearDrag * absV +
            forwardQuadraticDrag * absV * absV;

        // Oppose forward movement
        float dragLocalZ = -sign * dragMag;

        Vector3 dragLocal = new Vector3(0f, 0f, dragLocalZ);
        forwardDragForce = transform.TransformDirection(dragLocal);

        rb.AddForce(forwardDragForce, ForceMode.Force);
    }

    private void ApplyYawHydrodynamics()
    {
        yawRate = 0f;
        yawDampingTorque = 0f;

        if (yawDampingCoefficient == 0f && yawLateralCoupling == 0f)
            return;

        // World angular velocity
        Vector3 angVel = rb.angularVelocity;
        float yawVel = angVel.y; // rad/s around global up
        yawRate = yawVel;

        // Lateral speed in boat space
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        float vLat = localVel.x;

        // Base yaw damping: always opposes yawVel
        float baseTorque = -yawVel * yawDampingCoefficient;

        // Extra damping that scales with lateral slip, but STILL opposes yawVel
        float slipFactor = Mathf.Abs(vLat);
        float slipTorque = -yawVel * slipFactor * yawLateralCoupling;

        float totalTorque = baseTorque + slipTorque;
        yawDampingTorque = totalTorque;

        rb.AddTorque(Vector3.up * totalTorque, ForceMode.Acceleration);
    }

    private void ApplyHullDownforce()
    {
        hullDownforce = Vector3.zero;

        if (hullDownforceCoefficient <= 0f)
            return;

        // Forward speed in boat space
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        float fwd = Mathf.Max(0f, localVel.z);

        // Downforce grows with speed^exponent
        float magnitude = hullDownforceCoefficient * Mathf.Pow(fwd, hullDownforceSpeedExponent);

        // Apply downward force at COM
        hullDownforce = Vector3.down * magnitude;
        rb.AddForce(hullDownforce, ForceMode.Acceleration);
    }




}
