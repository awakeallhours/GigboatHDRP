using UnityEngine;

/// <summary>
/// Axiom Pitch Physics
/// Handles pitch torques that are not purely buoyancy:
/// - Hydrodynamic pitch damping (speed-scaled)
/// - Pitch torque from forward drag acting at an offset
/// 
/// This module is force/torque only. It never touches transform or velocity directly.
/// </summary>
public class PitchPhysics : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("References")]
    [Tooltip("Rigidbody for the boat. Assigned automatically if left empty.")]
    [SerializeField] private Rigidbody rb;

    [Tooltip("Hydrodynamics module providing forward drag force.")]
    [SerializeField] private Hydrodynamics hydrodynamics;

    [Tooltip("Point where forward drag is assumed to act (e.g., hull centre of drag).")]
    [SerializeField] private Transform dragApplicationPoint;

    // ─────────────────────────────────────────────────────────────
    // HYDRODYNAMIC PITCH DAMPING
    // ─────────────────────────────────────────────────────────────
    [Header("Pitch Damping")]
    [Tooltip("Base strength of pitch damping (torque per rad/s).")]
    [SerializeField] private float pitchDampingCoefficient = 10f;

    [Tooltip("Boat speed at which damping starts to ramp up.")]
    [SerializeField] private float dampingStartSpeed = 1f;

    [Tooltip("Boat speed at which damping reaches full effect.")]
    [SerializeField] private float dampingFullSpeed = 15f;

    [Tooltip("Scale factor applied at full damping speed (usually 1).")]
    [SerializeField] private float maxDampingScale = 1f;



    // ─────────────────────────────────────────────────────────────
    // DEBUG (READ ONLY)
    // ─────────────────────────────────────────────────────────────
    [Header("Debug (Read Only)")]
    [SerializeField] private float pitchRateRad;
    [SerializeField] private float pitchRateDeg;
    [SerializeField] private float dampingScale;
    [SerializeField] private Vector3 pitchDampingTorque;
    [SerializeField] private Vector3 dragPitchTorque;

    public float PitchRateDeg => pitchRateDeg;
    public Vector3 PitchDampingTorque => pitchDampingTorque;
    public Vector3 DragPitchTorque => dragPitchTorque;



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
        if (rb == null)
            return;

        ApplyHydrodynamicPitchDamping();
        ApplyDragInducedPitchTorque();
       
    }



    // ─────────────────────────────────────────────────────────────
    // HYDRODYNAMIC PITCH DAMPING
    // ─────────────────────────────────────────────────────────────
    private void ApplyHydrodynamicPitchDamping()
    {
        // Pitch rate around local X axis (radians/sec)
        pitchRateRad = rb.angularVelocity.x;
        pitchRateDeg = pitchRateRad * Mathf.Rad2Deg;

        // Boat speed in world space
        float speed = rb.linearVelocity.magnitude;

        // Damping scale based on speed (0..maxDampingScale)
        if (speed <= dampingStartSpeed)
        {
            dampingScale = 0f;
        }
        else
        {
            float t = Mathf.InverseLerp(dampingStartSpeed, dampingFullSpeed, speed);
            dampingScale = Mathf.Clamp01(t) * maxDampingScale;
        }

        if (dampingScale <= 0f || Mathf.Approximately(pitchRateRad, 0f))
        {
            pitchDampingTorque = Vector3.zero;
            return;
        }

        // Torque opposes pitch rate, scaled by speed-dependent damping
        float torqueX = -pitchRateRad * pitchDampingCoefficient * dampingScale;

        pitchDampingTorque = new Vector3(torqueX, 0f, 0f);
        rb.AddTorque(pitchDampingTorque, ForceMode.Acceleration);
    }



    // ─────────────────────────────────────────────────────────────
    // DRAG-INDUCED PITCH TORQUE
    // ─────────────────────────────────────────────────────────────
    private void ApplyDragInducedPitchTorque()
    {
        dragPitchTorque = Vector3.zero;

        if (hydrodynamics == null || dragApplicationPoint == null)
            return;

        Vector3 forwardDrag = hydrodynamics.ForwardDragForce;
        if (forwardDrag.sqrMagnitude < 1e-6f)
            return;

        // Lever arm from COM to the point where we pretend drag acts
        Vector3 r = dragApplicationPoint.position - rb.worldCenterOfMass;

        // Torque = r x F
        dragPitchTorque = Vector3.Cross(r, forwardDrag);

        rb.AddTorque(dragPitchTorque, ForceMode.Force);
    }

    
}