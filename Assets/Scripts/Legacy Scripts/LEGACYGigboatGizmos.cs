/*using UnityEngine;

/// <summary>
/// Draws editor‑only gizmos for the gig boat:
/// - Center of Mass
/// - Hull bottom reference
/// - Thrust point
/// - Velocity direction
/// - Lateral slip direction
///
/// This script is VISUAL ONLY.
/// It applies NO forces, NO torques, and NEVER runs in builds.
/// </summary>
public class GigboatGizmos : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // REQUIRED REFERENCES (assigned from GigboatMovement)
    // These are the ONLY dependencies the gizmo drawer needs.
    // ─────────────────────────────────────────────────────────────

    [Header("Required References")]
    [Tooltip("Rigidbody of the boat. Used to draw COM and velocity vectors.")]
    [SerializeField] private Rigidbody rb;

    [Tooltip("Thrust point transform (visualised in green).")]
    [SerializeField] private Transform thrustPoint;

    [Tooltip("Current thrust force vector (for debug arrow).")]
    [SerializeField] private Vector3 thrustPointForce;

    public void SetThrustForce(Vector3 force)
    {
        thrustPointForce = force;
    }


    // ─────────────────────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (rb == null)
            return;

        // ---------------------------------------------------------
        // 1. CENTER OF MASS VISUALISATION
        // ---------------------------------------------------------
        Vector3 com = rb.worldCenterOfMass;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(com, 0f);

        // Vertical line
        Gizmos.DrawLine(com + Vector3.up * 2f, com - Vector3.up * 2f);

        // Crosshair
        float cross = 0.5f;
        Gizmos.DrawLine(com + Vector3.right * cross, com - Vector3.right * cross);
        Gizmos.DrawLine(com + Vector3.forward * cross, com - Vector3.forward * cross);

        // ---------------------------------------------------------
        // 2. HULL BOTTOM REFERENCE (local Y = 0)
        // ---------------------------------------------------------
        float hullBottomLocalY = 0f;
        Vector3 hullBottom = transform.TransformPoint(new Vector3(0f, hullBottomLocalY, 0f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(hullBottom, new Vector3(0.15f, 0.02f, 0.15f));

        Gizmos.DrawLine(hullBottom, com);

        // ---------------------------------------------------------
        // 3. THRUST POINT VISUALISATION
        // ---------------------------------------------------------
        if (thrustPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(thrustPoint.position, 0.12f);

            // Vertical line
            Gizmos.DrawLine(thrustPoint.position + Vector3.up * 1.5f,
                            thrustPoint.position - Vector3.up * 1.5f);

            // Line to COM
            Gizmos.color = Color.white;
            Gizmos.DrawLine(thrustPoint.position, com);

            // Thrust vector arrow
            Gizmos.color = Color.red;
            Gizmos.DrawLine(thrustPoint.position,
                            thrustPoint.position + thrustPointForce * 0.01f);
        }

        // ---------------------------------------------------------
        // 4. VELOCITY + SLIP (runtime only)
        // ---------------------------------------------------------
        if (Application.isPlaying)
        {
            // Forward direction
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(com, com + transform.forward * 3f);

            Vector3 vel = rb.linearVelocity;

            if (vel.sqrMagnitude > 0.01f)
            {
                // Velocity direction
                Gizmos.color = Color.red;
                Gizmos.DrawLine(com, com + vel.normalized * 3f);

                // Lateral slip direction
                Vector3 localVel = transform.InverseTransformDirection(vel);
                Vector3 lateral = new Vector3(localVel.x, 0f, 0f);
                Vector3 lateralWorld = transform.TransformDirection(lateral);

                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(com, com + lateralWorld * 2f);
            }
        }
    }
}*/
