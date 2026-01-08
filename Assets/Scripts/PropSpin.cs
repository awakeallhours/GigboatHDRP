using UnityEngine;

/// <summary>
/// Purely visual propeller rotation driven by MarinePowertrainController.
/// No physics, no RPM modelling, no load modelling.
/// </summary>
public class PropSpin : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MarinePowertrainController powertrain;

    [Header("Settings")]
    [Tooltip("Multiplier for visual exaggeration (1 = real RPM).")]
    [SerializeField] private float visualRPMScale = 1f;

    private void Update()
    {
        if (powertrain == null)
            return;

        // Get physical RPM from powertrain
        float rpm = powertrain.EngineRPMPhysical * visualRPMScale;

        // Convert RPM → degrees per second
        float degreesPerSecond = (rpm / 60f) * 360f;

        // Rotate prop mesh
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}