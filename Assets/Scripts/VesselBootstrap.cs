using Axiom.Vessel;
using Axiom.Vessel.Diagnostics;
using System.Collections;
using UnityEngine;

/// <summary>
/// Bootstraps the vessel's orientation profile at runtime.
/// 
/// Responsibilities:
/// - Resolve required references (boat root, rigidbody, COB)
/// - Construct the <see cref="VesselOrientationDetector"/>
/// - Run the orientation detection coroutine
/// - Store the resulting <see cref="VesselOrientationProfile"/>
/// - Provide orientation data to all downstream systems (buoyancy, stability, controls)
/// 
/// IMPORTANT:
/// Orientation detection MUST complete before any system that depends on
/// roll/pitch/yaw axes begins operating. This class ensures that detection
/// runs automatically on startup.
/// </summary>
public class VesselBootstrap : MonoBehaviour
{
    // --------------------------------------------------------------------
    // Inspector References
    // --------------------------------------------------------------------

    [Header("References")]

    /// <summary>
    /// Root transform of the vessel. Used as the basis for orientation detection.
    /// If not assigned, defaults to this GameObject's transform.
    /// </summary>
    [SerializeField] private Transform boatRoot;

    /// <summary>
    /// Rigidbody associated with the vessel. Required for COM sampling.
    /// If not assigned, automatically resolved from <see cref="boatRoot"/>.
    /// </summary>
    [SerializeField] private Rigidbody rb;

    /// <summary>
    /// Centre‑of‑buoyancy component. Required for COB sampling.
    /// If not assigned, automatically resolved from <see cref="boatRoot"/>.
    /// </summary>
    [SerializeField] private BoatCOB cob;


    // --------------------------------------------------------------------
    // Public API
    // --------------------------------------------------------------------

    /// <summary>
    /// The detected vessel orientation profile.
    /// Populated once orientation detection completes.
    /// </summary>
    public VesselOrientationProfile Orientation
    {
        get; private set;
    }


    // --------------------------------------------------------------------
    // Internal State
    // --------------------------------------------------------------------

    /// <summary>
    /// The detector responsible for analysing the vessel's geometry and
    /// determining roll/pitch/yaw axes and sign conventions.
    /// </summary>
    private VesselOrientationDetector detector;


    // --------------------------------------------------------------------
    // Unity Lifecycle
    // --------------------------------------------------------------------

    /// <summary>
    /// Unity startup coroutine.
    /// Performs reference validation, constructs the detector, and runs
    /// orientation detection before any dependent systems begin operation.
    /// </summary>
    private IEnumerator Start()
    {
        // ------------------------------------------------------------
        // 1. Resolve references if not assigned in inspector
        // ------------------------------------------------------------
        if (boatRoot == null)
            boatRoot = transform;

        if (rb == null)
            rb = boatRoot.GetComponent<Rigidbody>();

        if (cob == null)
            cob = boatRoot.GetComponent<BoatCOB>();

        // ------------------------------------------------------------
        // 2. Construct the orientation detector
        // ------------------------------------------------------------
        detector = new VesselOrientationDetector(boatRoot, rb, cob);

        // ------------------------------------------------------------
        // 3. Run the detection coroutine
        //    This yields until the vessel's orientation profile is fully resolved.
        // ------------------------------------------------------------
        yield return StartCoroutine(detector.DetectOrientation(OnOrientationDetected));

        Debug.Log("VesselBootstrap: Orientation detection complete.");
    }


    // --------------------------------------------------------------------
    // Callbacks
    // --------------------------------------------------------------------

    /// <summary>
    /// Callback invoked when orientation detection completes.
    /// Stores the resulting profile and logs diagnostic information.
    /// </summary>
    private void OnOrientationDetected(VesselOrientationProfile profile)
    {
        Orientation = profile;

        Debug.Log($"Roll Axis: {profile.RollAxis}");
        Debug.Log($"Roll Direction: {profile.RollDirection}");
        Debug.Log($"Pitch Axis: {profile.PitchAxis}");
        Debug.Log($"Yaw Axis: {profile.YawAxis}");
    }
}