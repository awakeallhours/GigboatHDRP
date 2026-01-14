using UnityEngine;

public class FollowCam : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("References")]
    [Tooltip("The transform the camera rig rotates around.")]
    [SerializeField] private Transform cameraTarget;

    [Tooltip("The boat's Rigidbody used to read velocity.")]
    [SerializeField] private Rigidbody boatRb;


    // ─────────────────────────────────────────────────────────────
    // CAMERA BEHAVIOUR
    // ─────────────────────────────────────────────────────────────
    [Header("Camera Behaviour")]
    [Tooltip("How much sideways drift influences camera yaw.")]
    [Range(0f, 1f)]
    [SerializeField] private float sideDriftStrength = 0.08f;

    [Tooltip("How much forward/backward drift influences camera yaw.")]
    [Range(0f, 1f)]
    [SerializeField] private float forwardDriftStrength = 0.03f;


    // ─────────────────────────────────────────────────────────────
    // REVERSE FLIP LOGIC
    // ─────────────────────────────────────────────────────────────
    [Header("Reverse Flip")]
    [Tooltip("How quickly the camera rotates when switching forward/reverse.")]
    [SerializeField] private float reverseSlewSpeed = 2.5f;

    [Tooltip("Ignore tiny velocity changes within this range.")]
    [SerializeField] private float deadZone = 0.5f;

    [Tooltip("Velocity threshold (local Z) required to commit to reverse view.")]
    [SerializeField] private float reverseCommit = -1.5f;

    [Tooltip("Velocity threshold (local Z) required to commit to forward view.")]
    [SerializeField] private float forwardCommit = 1.0f;


    // ─────────────────────────────────────────────────────────────
    // INTERNAL STATE
    // ─────────────────────────────────────────────────────────────
    private float desiredYaw = 0f;
    private float smoothedYaw = 0f;


    // ─────────────────────────────────────────────────────────────
    // UPDATE LOOP
    // ─────────────────────────────────────────────────────────────
    private void LateUpdate()
    {
        if (cameraTarget == null || boatRb == null)
            return;

        if (cameraTarget.parent == null)
            return; // prevents null ref if hierarchy changes

        Vector3 localVel =
            cameraTarget.parent.InverseTransformDirection(boatRb.linearVelocity);

        // ---------------------------------------------------------
        // 1. Decide forward or reverse using commit thresholds
        // ---------------------------------------------------------
        if (localVel.z < reverseCommit)
        {
            desiredYaw = 180f; // committed to reversing
        }
        else if (localVel.z > forwardCommit)
        {
            desiredYaw = 0f;   // committed to moving forward
        }
        // else: dead zone → keep current desiredYaw

        // ---------------------------------------------------------
        // 2. Smoothly rotate toward that yaw
        // ---------------------------------------------------------
        smoothedYaw = Mathf.LerpAngle(
            smoothedYaw,
            desiredYaw,
            Time.deltaTime * reverseSlewSpeed
        );

        cameraTarget.localRotation = Quaternion.Euler(0f, smoothedYaw, 0f);

        // ---------------------------------------------------------
        // 3. Smooth drift values to avoid jitter
        // ---------------------------------------------------------
        float smoothedSide =
            Mathf.Lerp(0f, -localVel.x * sideDriftStrength, 0.5f);

        float smoothedForward =
            Mathf.Lerp(0f, -localVel.z * forwardDriftStrength, 0.5f);

        // ---------------------------------------------------------
        // 4. Apply drift AFTER smoothing
        // ---------------------------------------------------------
        cameraTarget.localRotation *= Quaternion.Euler(
            0f,
            smoothedSide + smoothedForward,
            0f
        );

        // ---------------------------------------------------------
        // 5. Remove roll from camera target
        // ---------------------------------------------------------
        cameraTarget.localRotation = Quaternion.Euler(
            cameraTarget.localEulerAngles.x,
            cameraTarget.localEulerAngles.y,
            0f
        );
    }

    public void ApplyFollowCamConfig(FollowCamConfig cfg)
    {
        sideDriftStrength = cfg.sideDriftStrength;
        forwardDriftStrength = cfg.forwardDriftStrength;

        reverseSlewSpeed = cfg.reverseSlewSpeed;
        deadZone = cfg.deadZone;
        reverseCommit = cfg.reverseCommit;
        forwardCommit = cfg.forwardCommit;
    }
}