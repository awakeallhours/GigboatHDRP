using UnityEngine;

/// <summary>
/// Drives the EngineController audio system using:
/// - Throttle from GigboatMovement
/// - RPM + Load from MarinePowertrainController
/// - Speed from Hydrodynamics
/// </summary>
public class BoatAudioDriver : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("References")]

    [Tooltip("Audio engine controller receiving RPM, load, throttle, etc.")]
    [SerializeField] private EngineController engine;

    [Tooltip("Movement script providing throttle input (-100 to +100).")]
    [SerializeField] private GigboatMovement gigboat;

    [Tooltip("Hydrodynamics component providing forward speed.")]
    [SerializeField] private Hydrodynamics hydro;

    [Tooltip("Marine powertrain providing physical RPM + load.")]
    [SerializeField] private MarinePowertrainController powertrain;



    // ─────────────────────────────────────────────────────────────
    // UPDATE LOOP
    // ─────────────────────────────────────────────────────────────
    private void Update()
    {
        if (engine == null || gigboat == null || powertrain == null)
            return;

        // ---------------------------------------------------------
        // 1. THROTTLE (0–1 forward, 0–1 reverse)
        // ---------------------------------------------------------
        float throttlePercent = gigboat.ThrottlePercent;

        float forward01 = Mathf.Clamp01(throttlePercent / 100f);
        float load01 = Mathf.Clamp01(Mathf.Abs(throttlePercent) / 100f);

        engine.SetThrottle(forward01);
        engine.SetLoad(load01);
        engine.SetReverse(throttlePercent < 0f);



        // ---------------------------------------------------------
        // 2. RPM (physical RPM from MarinePowertrain)
        // ---------------------------------------------------------
        engine.SetRPMFromPhysical(powertrain.EngineRPMPhysical);



        // ---------------------------------------------------------
        // 3. SPEED (forward speed from hydrodynamics)
        // ---------------------------------------------------------
        if (hydro != null)
            engine.SetSpeed(hydro.ForwardSpeed);
        else
            engine.SetSpeed(0f);
    }
}