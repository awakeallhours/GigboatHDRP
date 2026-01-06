using Unity.Cinemachine;
using UnityEngine;

public class FollowCam : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTarget;
    public Rigidbody boatRb;

    [Header("Camera Behaviour")]
    [Range(0f, 1f)] public float sideDriftStrength = 0.08f;
    [Range(0f, 1f)] public float forwardDriftStrength = 0.03f;

    [Header("Reverse Flip")]
    public float reverseSlewSpeed = 2.5f;

    // Reverse logic thresholds
    public float deadZone = 0.5f;         // ignore tiny velocity changes
    public float reverseCommit = -1.5f;   // must be reversing THIS much to flip
    public float forwardCommit = 1.0f;    // must be moving forward THIS much to flip back

    private float desiredYaw = 0f;
    private float smoothedYaw = 0f;

    void LateUpdate()
    {
        Vector3 localVel = cameraTarget.parent.InverseTransformDirection(boatRb.linearVelocity);

        // ----------------------------------------------------
        // 1. Decide forward or reverse using commit thresholds
        // ----------------------------------------------------
        if (localVel.z < reverseCommit)
        {
            desiredYaw = 180f; // committed to reversing
        }
        else if (localVel.z > forwardCommit)
        {
            desiredYaw = 0f;   // committed to moving forward
        }
        // else: stay where we are (dead zone)

        // ----------------------------------------------------
        // 2. Smoothly rotate toward that yaw
        // ----------------------------------------------------
        smoothedYaw = Mathf.LerpAngle(
            smoothedYaw,
            desiredYaw,
            Time.deltaTime * reverseSlewSpeed
        );

        cameraTarget.localRotation = Quaternion.Euler(0, smoothedYaw, 0);

        // ----------------------------------------------------
        // 3. Smooth drift values to avoid jitter
        // ----------------------------------------------------
        float smoothedSide = Mathf.Lerp(0, -localVel.x * sideDriftStrength, 0.5f);
        float smoothedForward = Mathf.Lerp(0, -localVel.z * forwardDriftStrength, 0.5f);

        // ----------------------------------------------------
        // 4. Apply drift AFTER smoothing
        // ----------------------------------------------------
        cameraTarget.localRotation *= Quaternion.Euler(0, smoothedSide + smoothedForward, 0);

        // Remove roll from camera target
        cameraTarget.localRotation = Quaternion.Euler(
            cameraTarget.localEulerAngles.x,
            cameraTarget.localEulerAngles.y,
            0f);


    }





}