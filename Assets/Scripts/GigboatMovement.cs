using UnityEngine;

/// <summary>
/// Primary helm control for the gigboat.
/// Handles throttle, rudder, and helm‑side debug values.
/// </summary>
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
    [SerializeField] private Hydrodynamics hydrodynamics;

    // ─────────────────────────────────────────────────────────────
    // THROTTLE (REALISTIC MARINE THROTTLE)
    // ─────────────────────────────────────────────────────────────
    [Header("Throttle")]
    [Tooltip("Rate at which the throttle lever moves (units per second).  \nSI‑integration: convert to a RateValue later.")]
    [SerializeField] private float throttleChangeRate = 60f;   // SI‑TODO

    [Tooltip("Dead‑zone around neutral where throttle snaps cleanly.")]
    [SerializeField] private float neutralGate = 2f;

    private float throttleMagnitude;      // internal smoothing

    // Lever / output
    public float ThrottlePercent
    {
        get; private set;
    }   // −100..+100
    public float TargetThrottle
    {
        get; private set;
    }   // −100..+100

    // ─────────────────────────────────────────────────────────────
    // KICK AHEAD / KICK ASTERN
    // ─────────────────────────────────────────────────────────────
    [Header("Kick Ahead / Kick Astern")]
    [Tooltip("Maximum time between taps to register a double‑tap.")]
    [SerializeField] private float doubleTapWindow = 0.25f;    // SI‑TODO

    private float lastTapForward = -1f;
    private float lastTapReverse = -1f;

    private bool overrideActive = false;
    private float overrideThrottle = 0f;   // −100..+100 during override
    private float savedThrottle = 0f;      // lever position before override

    // ─────────────────────────────────────────────────────────────
    // RUDDER
    // ─────────────────────────────────────────────────────────────
    [Header("Rudder Behaviour")]
    [SerializeField] private float rudderMaxStepPerSecond = 0.4f;   // SI‑TODO
    [SerializeField] private float rudderInputExponent = 2.5f;
    [SerializeField] private float rudderAuthorityLowSpeed = 2f;    // SI‑TODO
    [SerializeField] private float rudderResponseRate = 3f;         // SI‑TODO
    [SerializeField] private float rudderFadeSpeed = 10f;           // SI‑TODO
    [SerializeField] private float rudderMinResponse = 0.2f;
    [SerializeField] private float rudderMinEffectiveSpeed = 1f;    // SI‑TODO
    [SerializeField] private float maxRudderAngleDegrees = 30f;     // SI‑TODO

    [Header("Yaw Control")]
    [SerializeField] private float maxYawRateDeg = 12f;             // SI‑TODO
    [SerializeField] private float yawTurnDampingFactor = 0.4f;

    public float RudderAngle
    {
        get; private set;
    }      // −1..1
    public float YawRateDeg
    {
        get; private set;
    }
    public Rigidbody RB => rb;

    private float speed;
    private float forwardSpeed;

    public Transform CameraTarget => cameraTarget;
    public float SpeedKnots => speed * 1.943844f;       // SI‑TODO

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
        UpdateDebugValues();

        propWash.SetThrottle(ThrottlePercent);
        gizmoDrawer.SetThrustForce(Vector3.zero);
    }

    // ─────────────────────────────────────────────────────────────
    // THROTTLE (WITH KICK AHEAD / ASTERN)
    // ─────────────────────────────────────────────────────────────
    private void HandleThrottle()
    {
        float input = Input.GetAxisRaw("Vertical");

        // ─────────────────────────────────────────────
        // MODIFIER‑BASED KICK (Shift + W / Shift + S)
        // ─────────────────────────────────────────────
        bool kickAhead = Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.W);
        bool kickAstern = Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.S);

        if (kickAhead)
        {
            overrideActive = true;
            overrideThrottle = 100f;

            Debug.Log("[Gigboat] Kick Ahead (modifier)");
        }
        else if (kickAstern)
        {
            overrideActive = true;
            overrideThrottle = -100f;

            Debug.Log("[Gigboat] Kick Astern (modifier)");
        }
        else if (overrideActive)
        {
            // Modifier released → go to neutral
            overrideActive = false;
            overrideThrottle = 0f;
            TargetThrottle = 0f;
            throttleMagnitude = 0f;
            ThrottlePercent = 0f;

            Debug.Log("[Gigboat] Kick Released → Neutral");
        }

        // If override is active, bypass everything
        if (overrideActive)
        {
            ThrottlePercent = overrideThrottle;
            return;
        }

        // ─────────────────────────────────────────────
        // NORMAL THROTTLE LOGIC
        // ─────────────────────────────────────────────
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

        if (hydrodynamics != null)
            hydrodynamics.RudderAngleDegrees = -RudderAngle * maxRudderAngleDegrees;
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