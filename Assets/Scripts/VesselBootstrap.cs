using Axiom.Vessel;
using Axiom.Vessel.Diagnostics;
using System.Collections;
using UnityEngine;

public class VesselBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform boatRoot;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private StabilityProfileComponent stabilityProfileComponent;

    [Header("Diagnostics / Buoyancy / Powertrain")]
    [SerializeField] private BoatCOB cob;
    [SerializeField] private BoatCOM com;
    [SerializeField] private WaterProbeSampler probeSampler;
    [SerializeField] private MarinePowertrainController powertrain;

    // ORIENTATION
    public VesselOrientationProfile Orientation
    {
        get; private set;
    }



    // STABILITY
    public StabilityProfile Stability
    {
        get; private set;
    }



    // PROVIDERS
    public VelocityProvider Velocity
    {
        get; private set;
    }
    public ProbeDepthProvider ProbeDepth
    {
        get; private set;
    }
    public MassPropertiesProvider MassProps
    {
        get; private set;
    }
    public ThrustProvider Thrust
    {
        get; private set;
    }
    public AxesProvider Axes
    {
        get; private set;
    }
    public StabilityProvider StabilityProv
    {
        get; private set;
    }

    public bool IsReady
    {
        get; private set;
    }

    private VesselOrientationDetector detector;

    private void Awake()
    {
        // Your original reference resolution, extended minimally
        if (boatRoot == null)
            boatRoot = transform;

        if (rb == null)
            rb = boatRoot.GetComponent<Rigidbody>();

        if (cob == null)
            cob = GetComponent<BoatCOB>();

        if (com == null)
            com = GetComponent<BoatCOM>();

        if (probeSampler == null)
            probeSampler = GetComponent<WaterProbeSampler>();

        if (powertrain == null)
            powertrain = GetComponent<MarinePowertrainController>();
    }

    private IEnumerator Start()
    {
        // 1. Resolve references (your original logic)
        if (boatRoot == null)
            boatRoot = transform;

        if (rb == null)
            rb = boatRoot.GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("[VesselBootstrap] Rigidbody missing — orientation detection cannot run.");
            yield break;
        }

        // 2. Construct detector (your original)
        detector = new VesselOrientationDetector(boatRoot, rb);

        // 3. Run detection (your original)
        yield return StartCoroutine(detector.DetectOrientation(OnOrientationDetected));

        Debug.Log("VesselBootstrap: Orientation detection complete.");

        // ─────────────────────────────────────────────
        // 4. Wait for Orientation to be assigned
        // ─────────────────────────────────────────────
        while (Orientation.RollAxis == Vector3.zero)
        {
            yield return null;
        }

        // ─────────────────────────────────────────────
        // 5. Run GM/GZ stability scan (NEW)
        // ─────────────────────────────────────────────
        if (cob == null || com == null)
        {
            Debug.LogWarning("[VesselBootstrap] Missing BoatCOB or BoatCOM — stability scan will be skipped.");
        }
        else
        {
            var scanner = new GMGZStabilityScanner(
                this,
                boatRoot,
                rb,
                cob,
                com
            );

            yield return StartCoroutine(scanner.RunScan(
                startAngle: -40f,
                endAngle: 40f,
                step: 2f,
                settleTime: 0.1f,
                onComplete: OnStabilityDetected
            ));
        }

        // Wait for stability if it ran
        // (If skipped, Stability will remain null and StabilityProv will be null)
        // This avoids blocking forever if scan is disabled/misconfigured.
        // You can tighten this later if you want stability to be mandatory.
        // For now: soft dependency.
        // ─────────────────────────────────────────────
        // 6. Construct providers (NEW)
        // ─────────────────────────────────────────────
        Velocity = new VelocityProvider(rb);

        if (probeSampler != null)
            ProbeDepth = new ProbeDepthProvider(probeSampler);
        else
            Debug.LogWarning("[VesselBootstrap] No WaterProbeSampler found — ProbeDepthProvider will be null.");

        MassProps = new MassPropertiesProvider(rb);

        if (powertrain != null)
            Thrust = new ThrustProvider(powertrain);
        else
            Debug.LogWarning("[VesselBootstrap] No MarinePowertrainController found — ThrustProvider will be null.");

        Axes = new AxesProvider(Orientation, boatRoot);

        StabilityProv = new StabilityProvider(stabilityProfileComponent.Profile);

        IsReady = true;
        Debug.Log("[VesselBootstrap] Vessel is fully initialised (orientation + providers).");
    }

    private void OnOrientationDetected(VesselOrientationProfile profile)
    {
        Orientation = profile;

        // Your original logging preserved
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

    private void OnStabilityDetected(StabilityProfile profile)
    {
        Stability = profile;
        stabilityProfileComponent.SetProfile(profile);

        Debug.Log("[VesselBootstrap] GM/GZ stability scan complete.");
    }
}