using UnityEngine;

/// <summary>
/// Centralised debug data provider for the gig boat.
/// Exposes propulsion, hydrodynamics, buoyancy, and transform metrics
/// to UI debug panels and diagnostic tools.
/// </summary>
public class GigboatDebugProbe : MonoBehaviour
{
    // --- Public Accessors (Debug UI reads from these) ---

    public Buoyancy Buoyancy => buoyancy;
    public GigboatMovement Movement => gigboat;
    public Hydrodynamics WaterForces => hydrodynamics;
    public Transform[] Points => points;
    public Rigidbody RB => rb;

    // --- References ---

    [Header("Core References")]

    [Tooltip("Buoyancy system providing wave-aware depth sampling.")]
    [SerializeField] private Buoyancy buoyancy;

    [Tooltip("Movement controller providing throttle, rudder, and yaw data.")]
    [SerializeField] private GigboatMovement gigboat;

    [Tooltip("Propeller spin system providing RPM data.")]
    [SerializeField] private PropSpin propSpin;

    [Tooltip("Hydrodynamics system providing drag and planing data.")]
    [SerializeField] private Hydrodynamics hydrodynamics;

    [Tooltip("Buoyancy probe transforms used for depth visualisation.")]
    [SerializeField] private Transform[] points;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // --- Propulsion Debug ---

    /// <summary>Throttle input as a 0–1 value.</summary>
    public float ThrottlePercent => gigboat.ThrottlePercent;

    /// <summary>Current propeller RPM.</summary>
    public float RPM => propSpin.currentRPM;

    // --- Orientation Debug ---

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

    // --- Water Depth Sampling ---

    /// <summary>
    /// Returns the depth of a buoyancy probe relative to the water surface.
    /// Uses wave height if a Water component is present, otherwise falls back to flat water level.
    /// </summary>
    public float GetPointDepth(Transform p)
    {
        // Water system disabled — assume flat water at Y = 0
        float waterHeight = 0f;
        return waterHeight - p.position.y;
    }


    // --- Roll Damping Debug ---

    public float RollDamping => gigboat.RollDampingStrength;
    public float RollStiffnessBase => gigboat.RollStiffnessBase;
    public float RollStiffnessSpeed => gigboat.RollStiffnessSpeedMultiplier;
    public float RudderRoll => gigboat.RudderRollTorqueStrength;
    public float RudderRollThreshold => gigboat.RudderRollActivationSpeed;

    // --- Hydrodynamics Debug ---

    public float ForwardSpeed => hydrodynamics.ForwardSpeed;
    public Vector3 ForwardDragForce => hydrodynamics.ForwardDragForce;
    public float ForwardDragMagnitude => hydrodynamics.ForwardDragForce.magnitude;

    public float LateralSpeed => hydrodynamics.LateralSpeed;
    public Vector3 LateralDragForce => hydrodynamics.LateralDragForce;
    public float LateralDragMagnitude => hydrodynamics.LateralDragForce.magnitude;

    public float YawRateHydro => hydrodynamics.YawRate;
    public float YawDampingTorque => hydrodynamics.YawDampingTorque;

    public bool IsPlaning => hydrodynamics.IsPlaning;

    // --- Utility ---

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