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

    [Tooltip("Buoyancy system providing wave-aware depth sampling.")]
    [SerializeField] private Buoyancy buoyancy;

    [Tooltip("Movement controller providing throttle, rudder, and yaw data.")]
    [SerializeField] private GigboatMovement gigboat;

    [Tooltip("Hydrodynamics system providing drag and planing data.")]
    [SerializeField] private Hydrodynamics hydrodynamics;

    [Tooltip("Buoyancy probe transforms used for depth visualisation.")]
    [SerializeField] private Transform[] points;

    [Tooltip("Rigidbody of the boat.")]
    [SerializeField] private Rigidbody rb;

    [Tooltip("Marine powertrain providing engine RPM + load.")]
    [SerializeField] private MarinePowertrainController powertrain;



    // ─────────────────────────────────────────────────────────────
    // PUBLIC ACCESSORS (Debug UI reads from these)
    // ─────────────────────────────────────────────────────────────

    public Buoyancy Buoyancy => buoyancy;
    public GigboatMovement Movement => gigboat;
    public Hydrodynamics WaterForces => hydrodynamics;
    public Transform[] Points => points;
    public Rigidbody RB => rb;

    /// <summary>Throttle input as a -100 to +100 value.</summary>
    public float ThrottlePercent => gigboat.ThrottlePercent;

    /// <summary>Current physical engine RPM from MarinePowertrain.</summary>
    public float RPM => powertrain != null ? powertrain.EngineRPMPhysical : 0f;



    // ─────────────────────────────────────────────────────────────
    // ORIENTATION DEBUG
    // ─────────────────────────────────────────────────────────────

    /// <summary>Boat roll angle in degrees (-180 to 180).</summary>
    public float Roll => Mathf.DeltaAngle(0, transform.eulerAngles.z);

    /// <summary>Boat pitch angle in degrees (-180 to 180).</summary>
    public float Pitch => Mathf.DeltaAngle(0, transform.eulerAngles.x);

    /// <summary>Vertical position of the boat's center of mass.</summary>
    public float Heave => rb.worldCenterOfMass.y;

    /// <summary>Current rudder angle in degrees.</summary>
    public float RudderAngle => gigboat.RudderAngle;

    /// <summary>Yaw rate in degrees per second.</summary>
    public float YawRateDeg => gigboat.YawRateDeg;



    // ─────────────────────────────────────────────────────────────
    // WATER DEPTH SAMPLING
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the depth of a buoyancy probe relative to the water surface.
    /// Uses wave height if a Water component is present, otherwise flat water at Y=0.
    /// </summary>
    public float GetPointDepth(Transform p)
    {
        float waterHeight = 0f; // placeholder until wave system is wired
        return waterHeight - p.position.y;
    }



    /*// ─────────────────────────────────────────────────────────────
    // ROLL DAMPING DEBUG (values pulled from GigboatMovement)
    // ─────────────────────────────────────────────────────────────

    public float RollDamping => gigboat.RollDampingStrength;
    public float RollStiffnessBase => gigboat.RollStiffnessBase;
    public float RollStiffnessSpeed => gigboat.RollStiffnessSpeedMultiplier;
    public float RudderRoll => gigboat.RudderRollTorqueStrength;
    public float RudderRollThreshold => gigboat.RudderRollActivationSpeed;*/



    // ─────────────────────────────────────────────────────────────
    // HYDRODYNAMICS DEBUG
    // ─────────────────────────────────────────────────────────────

    public float ForwardSpeed => hydrodynamics.ForwardSpeed;
    public Vector3 ForwardDragForce => hydrodynamics.ForwardDragForce;
    public float ForwardDragMagnitude => hydrodynamics.ForwardDragForce.magnitude;

    public float LateralSpeed => hydrodynamics.LateralSpeed;
    public Vector3 LateralDragForce => hydrodynamics.LateralDragForce;
    public float LateralDragMagnitude => hydrodynamics.LateralDragForce.magnitude;

    public float YawRateHydro => hydrodynamics.YawRate;
    public float YawDampingTorque => hydrodynamics.YawDampingTorque;

    



    // ─────────────────────────────────────────────────────────────
    // UTILITY
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Forces all buoyancy probe transforms to sit at local Y = 0.
    /// Useful when adjusting hull probe layout.
    /// </summary>
    [ContextMenu("Apply Buoyancy Point Offset")]
    private void ApplyOffset()
    {
        foreach (Transform p in points)
        {
            Vector3 local = p.localPosition;
            local.y = 0f;
            p.localPosition = local;
        }
    }
}