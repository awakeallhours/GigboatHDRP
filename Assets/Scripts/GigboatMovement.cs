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

    // NEW: Hydrodynamics reference for rudder angle injection
    [SerializeField] private Hydrodynamics hydrodynamics;



    // ─────────────────────────────────────────────────────────────
    // THROTTLE (REALISTIC MARINE THROTTLE — CLEANED)
    // ─────────────────────────────────────────────────────────────
    [Header("Throttle")]
    [SerializeField] private float throttleChangeRate = 60f;
    [SerializeField] private float neutralGate = 2f;

    private float throttleMagnitude;



    // ─────────────────────────────────────────────────────────────
    // PITCH CONTROL
    // ─────────────────────────────────────────────────────────────
    [Header("Pitch & Trim")]
    [SerializeField] private float pitchDampingStrength = 10f;



    // ─────────────────────────────────────────────────────────────
    // RUDDER CONTROL
    // ─────────────────────────────────────────────────────────────
    [Header("Rudder Behaviour")]
    [SerializeField] private float rudderMaxStepPerSecond = 0.4f;
    [SerializeField] private float rudderInputExponent = 2.5f;
    [SerializeField] private float rudderAuthorityLowSpeed = 2f;
    [SerializeField] private float rudderResponseRate = 3f;
    [SerializeField] private float rudderFadeSpeed = 10f;
    [SerializeField] private float rudderMinResponse = 0.2f;
    [SerializeField] private float rudderMinEffectiveSpeed = 1f;

    // NEW: authoritative physical rudder angle range (degrees)
    [SerializeField] private float maxRudderAngleDegrees = 30f;



    // ─────────────────────────────────────────────────────────────
    // YAW CONTROL
    // ─────────────────────────────────────────────────────────────
    [Header("Yaw Control")]
    [SerializeField] private float maxYawRateDeg = 12f;
    [SerializeField] private float yawTurnDampingFactor = 0.4f;



    // ─────────────────────────────────────────────────────────────
    // PUBLIC PROPERTIES
    // ─────────────────────────────────────────────────────────────
    public float RudderAngle
    {
        get; private set;
    }     // normalized -1..1
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
        HandlePitchPhysics();
        UpdateDebugValues();

        propWash.SetPropDepth(buoyancy.SternSubmerged01);
        propWash.SetThrottle(ThrottlePercent);

        gizmoDrawer.SetThrustForce(Vector3.zero);
    }



    // ─────────────────────────────────────────────────────────────
    // THROTTLE
    // ─────────────────────────────────────────────────────────────
    private void HandleThrottle()
    {
        float input = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(TargetThrottle) < neutralGate)
        {
            if (Mathf.Abs(input) < 0.1f)
                input = 0f;
        }

        if (input != 0f)
        {
            TargetThrottle += input * throttleChangeRate * Time.fixedDeltaTime;
            TargetThrottle = Mathf.Clamp(TargetThrottle, -100f, 100f);
        }

        float targetDirection = Mathf.Sign(TargetThrottle);
        float targetMagnitude = Mathf.Abs(TargetThrottle);

        throttleMagnitude = Mathf.MoveTowards(
            throttleMagnitude,
            targetMagnitude,
            throttleChangeRate * Time.fixedDeltaTime
        );

        ThrottlePercent = targetDirection * throttleMagnitude;
    }



    // ─────────────────────────────────────────────────────────────
    // RUDDER CONTROL
    // ─────────────────────────────────────────────────────────────
    private void HandleRudder()
    {
        float input = Input.GetAxisRaw("Horizontal");

        float commanded = Mathf.Sign(input) * Mathf.Pow(Mathf.Abs(input), rudderInputExponent);

        float speed = rb.linearVelocity.magnitude;
        float fade = Mathf.InverseLerp(rudderFadeSpeed, rudderMinEffectiveSpeed, speed);
        float authority = Mathf.Lerp(rudderMinResponse, rudderAuthorityLowSpeed, fade);

        float delta = commanded * authority * rudderResponseRate * Time.fixedDeltaTime;

        float maxStep = rudderMaxStepPerSecond * Time.fixedDeltaTime;
        delta = Mathf.Clamp(delta, -maxStep, maxStep);

        RudderAngle += delta;
        RudderAngle = Mathf.Clamp(RudderAngle, -1f, 1f);

        // NEW: Feed Hydrodynamics the real rudder angle in degrees
        if (hydrodynamics != null)
            hydrodynamics.RudderAngleDegrees = -RudderAngle * maxRudderAngleDegrees;
    }



    // ─────────────────────────────────────────────────────────────
    // YAW PHYSICS
    // ─────────────────────────────────────────────────────────────
    private void HandleYawPhysics()
    {
        float speed = rb.linearVelocity.magnitude;

        float yawCommand = RudderAngle * maxYawRateDeg;

        float damping = 1f / (1f + speed * yawTurnDampingFactor);
        yawCommand *= damping;

        rb.AddTorque(Vector3.up * yawCommand, ForceMode.Acceleration);

        YawRateDeg = rb.angularVelocity.y * Mathf.Rad2Deg;
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