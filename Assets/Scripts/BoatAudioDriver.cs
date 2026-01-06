using UnityEngine;

public class BoatAudioDriver : MonoBehaviour
{
    public EngineController engine;
    public GigboatMovement gigboat;              // your movement script
    public PropSpin propSpin;            // where RPM comes from
    public Hydrodynamics hydro;          // where forwardSpeed comes from

    
    


    void Update()
    {
        // 1. Throttle (0–1)
        float throttlePercent = gigboat.ThrottlePercent;

        // Forward throttle (0–1)
        float forward01 = Mathf.Clamp01(throttlePercent / 100f);

        // Reverse throttle (0–1)
        float reverse01 = Mathf.Clamp01(-throttlePercent / 100f);

        // Engine load should be the magnitude of throttle
        float load01 = Mathf.Clamp01(Mathf.Abs(throttlePercent) / 100f);

        engine.SetThrottle(forward01);     // forward engine layers
        engine.SetLoad(load01);            // reverse layer uses load
        engine.SetReverse(throttlePercent < 0f);

        // 2. RPM (normalized)
        engine.SetRPMFromPhysical(propSpin.currentRPM);

        // 3. Speed (raw for now)
        engine.SetSpeed(hydro.ForwardSpeed);


        engine.SetReverse(gigboat.ThrottlePercent < 0f);

    }



}