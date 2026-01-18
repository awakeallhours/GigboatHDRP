#if UNITY_EDITOR
using Axiom.Physics.Units;
using UnityEngine;

[RequireComponent(typeof(Buoyancy))]
public class BuoyancyAutoCalibrator : MonoBehaviour
{
    [Header("Calibration Settings")]
    [Tooltip("Enable to run auto-calibration once when equilibrium is detected.")]
    public bool enableCalibration = true;

    [Tooltip("How close buoyancy must be to weight to count as equilibrium.")]
    public float forceTolerancePercent = 0.05f; // 5%

    [Tooltip("How long equilibrium must be maintained before calibration triggers.")]
    public float equilibriumHoldTime = 0.5f;

    private Buoyancy buoyancy;
    private Rigidbody rb;

    private float equilibriumTimer = 0f;
    private bool calibrationDone = false;

    private void Awake()
    {
        buoyancy = GetComponent<Buoyancy>();
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!enableCalibration || calibrationDone)
            return;

        float weight = rb.mass * Physics.gravity.magnitude;
        float buoyantForce = buoyancy.TotalBuoyancyForce;

        float diff = Mathf.Abs(buoyantForce - weight);
        float tolerance = weight * forceTolerancePercent;

        bool inEquilibrium = diff < tolerance && buoyantForce > 0f;

        if (inEquilibrium)
        {
            equilibriumTimer += Time.fixedDeltaTime;

            if (equilibriumTimer >= equilibriumHoldTime)
            {
                Debug.Log($"[Calibrator] Equilibrium detected. " +
                          $"Buoyancy={buoyantForce:F3}, Weight={weight:F3}, Diff={diff:F3}");

                PerformCalibration();
                calibrationDone = true;
                enableCalibration = false;

                Debug.Log("[Calibrator] Calibration complete. Component disabled.");
            }
        }
        else
        {
            equilibriumTimer = 0f;
        }
    }

    private void PerformCalibration()
    {
        float sumDepth = 0f;

        for (int i = 0; i < buoyancy.SamplePoints.Length; i++)
        {
            if (!buoyancy.PointValid[i])
                continue;

            if (buoyancy.ProbeTypes[i] == ProbeType.Deck)
                continue;

            float waterY = buoyancy.PointHeights[i];
            float pointY = buoyancy.SamplePoints[i].position.y;
            float depth = waterY - pointY;

            if (depth > 0f)
                sumDepth += depth;
        }

        if (sumDepth <= 0f)
        {
            Debug.LogError("[Calibrator] ERROR: No submerged probes found. Cannot calibrate.");
            return;
        }

        float mass = rb.mass;
        float rho = buoyancy.WaterDensity.ValueKgPerCubicMeter;

        float solvedProbeArea = mass / (rho * sumDepth);

        Debug.Log($"[Calibrator] sumDepth={sumDepth:F4}, mass={mass}, rho={rho}");
        Debug.Log($"[Calibrator] SOLVED probeArea = {solvedProbeArea:F6} m^2");

        buoyancy.ProbeArea.inputValue = solvedProbeArea;
        buoyancy.ProbeArea.unit = AreaUnit.SquareMeters;


        Debug.Log("[Calibrator] probeArea applied to Buoyancy component.");
    }
}
#endif
