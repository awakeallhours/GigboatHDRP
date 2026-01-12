using UnityEngine;

/// <summary>
/// Centralised debug data provider for the gig boat.
/// Exposes propulsion, hydrodynamics, buoyancy, and transform metrics
/// to UI debug panels and diagnostic tools.
/// </summary>
public class GigboatDebugProbe : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // CORE REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("Core References")]
    [SerializeField] private Buoyancy buoyancy;
    [SerializeField] private GigboatMovement gigboat;
    [SerializeField] private Hydrodynamics hydrodynamics;
    [SerializeField] private MarinePowertrainController powertrain;
    [SerializeField] private Rigidbody rb;

    [Tooltip("Buoyancy probe transforms used for depth visualisation.")]
    [SerializeField] private Transform[] points;


    // ─────────────────────────────────────────────────────────────
    // PUBLIC ACCESSORS (Debug UI reads from these)
    // ─────────────────────────────────────────────────────────────
    public Buoyancy Buoyancy => buoyancy;
    public GigboatMovement Movement => gigboat;
    public Hydrodynamics WaterForces => hydrodynamics;
    public MarinePowertrainController Powertrain => powertrain;
    public Rigidbody RB => rb;
    public Transform[] Points => points;

    /// <summary>Throttle input as a -100 to +100 value.</summary>
    public float ThrottlePercent => gigboat != null ? gigboat.ThrottlePercent : 0f;

    /// <summary>Current physical engine RPM from MarinePowertrain.</summary>
    public float RPM => powertrain != null ? powertrain.EngineRPMPhysical : 0f;

    /// <summary>Yaw rate from hydrodynamics (rad/s), debug only.</summary>
    public float YawRateHydro => hydrodynamics != null ? hydrodynamics.YawRate : 0f;

    /// <summary>Yaw damping torque from hydrodynamics (debug only).</summary>
    public float YawDampingTorque => hydrodynamics != null ? hydrodynamics.YawDampingTorque : 0f;


    // ─────────────────────────────────────────────────────────────
    // ORIENTATION DEBUG
    // ─────────────────────────────────────────────────────────────
    public float Roll => Mathf.DeltaAngle(0f, transform.eulerAngles.z);
    public float Pitch => Mathf.DeltaAngle(0f, transform.eulerAngles.x);
    public float Heave => rb != null ? rb.worldCenterOfMass.y : 0f;

    /// <summary>Current rudder angle in degrees (-1..1 mapped externally).</summary>
    public float RudderAngle => gigboat != null ? gigboat.RudderAngle : 0f;

    /// <summary>Yaw rate in degrees per second (from movement controller).</summary>
    public float YawRateDeg => gigboat != null ? gigboat.YawRateDeg : 0f;


    // ─────────────────────────────────────────────────────────────
    // WATER DEPTH SAMPLING
    // ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Returns the depth of a buoyancy probe relative to the water surface.
    /// Placeholder until wave system is wired.
    /// </summary>
    public float GetPointDepth(Transform p)
    {
        float waterHeight = 0f; // placeholder
        return waterHeight - p.position.y;
    }


    // ─────────────────────────────────────────────────────────────
    // HYDRODYNAMICS DEBUG
    // ─────────────────────────────────────────────────────────────
    public float ForwardSpeed => hydrodynamics != null ? hydrodynamics.ForwardSpeed : 0f;
    public Vector3 ForwardDragForce => hydrodynamics != null ? hydrodynamics.ForwardDragForce : Vector3.zero;
    public float ForwardDragMagnitude => ForwardDragForce.magnitude;

    public float LateralSpeed => hydrodynamics != null ? hydrodynamics.LateralSpeed : 0f;
    public Vector3 LateralDragForce => hydrodynamics != null ? hydrodynamics.LateralDragForce : Vector3.zero;
    public float LateralDragMagnitude => LateralDragForce.magnitude;


    // ─────────────────────────────────────────────────────────────
    // UTILITY
    // ─────────────────────────────────────────────────────────────
    [ContextMenu("Apply Buoyancy Point Offset")]
    private void ApplyOffset()
    {
        if (points == null) return;

        foreach (Transform p in points)
        {
            if (p == null) continue;

            Vector3 local = p.localPosition;
            local.y = 0f;
            p.localPosition = local;
        }
    }
}