using Axiom.Vessel;
using Axiom.Vessel.Diagnostics;
using System.Collections;
using UnityEngine;

public class VesselBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform boatRoot;
    [SerializeField] private Rigidbody rb;

    public VesselOrientationProfile Orientation
    {
        get; private set;
    }

    private VesselOrientationDetector detector;

    private IEnumerator Start()
    {
        // 1. Resolve references
        if (boatRoot == null)
            boatRoot = transform;

        if (rb == null)
            rb = boatRoot.GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("[VesselBootstrap] Rigidbody missing — orientation detection cannot run.");
            yield break;
        }

        // 2. Construct detector
        detector = new VesselOrientationDetector(boatRoot, rb);

        // 3. Run detection
        yield return StartCoroutine(detector.DetectOrientation(OnOrientationDetected));

        Debug.Log("VesselBootstrap: Orientation detection complete.");
    }

    private void OnOrientationDetected(VesselOrientationProfile profile)
    {
        Orientation = profile;

        Debug.Log($"Roll Axis: {profile.RollAxis}  (dir {profile.RollDirection})");
        Debug.Log($"Pitch Axis: {profile.PitchAxis} (dir {profile.PitchDirection})");
        Debug.Log($"Yaw Axis: {profile.YawAxis}   (dir {profile.YawDirection})");

        if (profile.Warnings != null && profile.Warnings.Length > 0)
        {
            Debug.LogWarning("Orientation Warnings:");
            foreach (var w in profile.Warnings)
                Debug.LogWarning(" • " + w);
        }
    }
}