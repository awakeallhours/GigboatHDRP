using Axiom.Vessel;
using Axiom.Vessel.Diagnostics;
using System.Collections;
using UnityEngine;

public class VesselBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform boatRoot;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private BoatCOB cob;

    public VesselOrientationProfile Orientation
    {
        get; private set;
    }

    private VesselOrientationDetector detector;

    private IEnumerator Start()
    {
        // Safety checks (optional for now)
        if (boatRoot == null)
            boatRoot = transform;

        if (rb == null)
            rb = boatRoot.GetComponent<Rigidbody>();

        if (cob == null)
            cob = boatRoot.GetComponent<BoatCOB>();

        // Create the detector
        detector = new VesselOrientationDetector(boatRoot, rb, cob);

        // Run the orientation scan
        yield return StartCoroutine(detector.DetectOrientation(OnOrientationDetected));

        Debug.Log("VesselBootstrap: Orientation detection complete.");
    }

    private void OnOrientationDetected(VesselOrientationProfile profile)
    {
        Orientation = profile;

        Debug.Log($"Roll Axis: {profile.RollAxis}");
        Debug.Log($"Roll Direction: {profile.RollDirection}");
        Debug.Log($"Pitch Axis: {profile.PitchAxis}");
        Debug.Log($"Yaw Axis: {profile.YawAxis}");
    }
}