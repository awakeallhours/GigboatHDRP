using UnityEngine;

/// <summary>
/// Purely visual rudder animation driven by GigboatMovement.
/// Does not affect physics or steering forces.
/// </summary>
public class RudderController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("References")]

    [Tooltip("Movement controller providing the normalized rudder angle (-1 to +1).")]
    [SerializeField] private GigboatMovement movement;



    // ─────────────────────────────────────────────────────────────
    // VISUAL SETTINGS
    // ─────────────────────────────────────────────────────────────
    [Header("Visual Settings")]

    [Tooltip("Maximum visual rotation angle of the rudder in degrees.")]
    [SerializeField] private float maxVisualAngle = 30f;

    [Tooltip("How quickly the rudder visually turns toward the target angle.")]
    [SerializeField] private float visualTurnSpeed = 5f;



    // ─────────────────────────────────────────────────────────────
    // INTERNAL STATE
    // ─────────────────────────────────────────────────────────────
    private float currentAngle;



    // ─────────────────────────────────────────────────────────────
    // UPDATE LOOP
    // ─────────────────────────────────────────────────────────────
    private void Update()
    {
        if (movement == null)
            return;

        // Convert normalized rudder input (-1 to +1) into a visual angle
        float targetAngle = movement.RudderAngle * maxVisualAngle;

        // Smooth interpolation for nicer animation
        currentAngle = Mathf.Lerp(
            currentAngle,
            targetAngle,
            Time.deltaTime * visualTurnSpeed
        );

        // Apply rotation to the rudder mesh
        transform.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
    }
}