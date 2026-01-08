using UnityEngine;

/// <summary>
/// Bridges MarinePowertrainController → AudioEngineController.
/// Sends normalized RPM, load, throttle, reverse, and speed to the audio system.
/// </summary>
public class BoatAudioDriver : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("References")]

    [Tooltip("Audio engine controller receiving normalized values.")]
    [SerializeField] private AudioEngineController audioEngine;

    [Tooltip("Marine powertrain providing physical + normalized engine state.")]
    [SerializeField] private MarinePowertrainController powertrain;

    [Tooltip("Hydrodynamics component providing forward speed (optional).")]
    [SerializeField] private Hydrodynamics hydro;


    // ─────────────────────────────────────────────────────────────
    // UPDATE LOOP
    // ─────────────────────────────────────────────────────────────
    private void Update()
    {
        if (audioEngine == null || powertrain == null)
            return;

        // ---------------------------------------------------------
        // 1. NORMALIZED ENGINE STATE (from powertrain)
        // ---------------------------------------------------------
        audioEngine.SetRPM01(powertrain.EngineRPM01);
        audioEngine.SetLoad01(powertrain.EngineLoad01);
        audioEngine.SetThrottle01(powertrain.Throttle01);

        // Reverse flag based on physical RPM sign
        audioEngine.SetReverse(powertrain.EngineRPMPhysical < 0f);

        // ---------------------------------------------------------
        // 2. SPEED (from hydrodynamics or fallback)
        // ---------------------------------------------------------
        float speed = 0f;

        if (hydro != null)
            speed = hydro.ForwardSpeed;
        else
            speed = powertrain.GetComponent<Rigidbody>().linearVelocity.magnitude;

        audioEngine.SetSpeed(speed);
    }
}