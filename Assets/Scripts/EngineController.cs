using UnityEngine;

public class EngineController : MonoBehaviour
{
    [Header("Normalized parameters (0–1)")]
    [Range(0f, 1f)] public float engineRPM = 0.0f;
    [Range(0f, 1f)] public float engineLoad = 0.0f;
    [Range(0f, 1f)] public float throttle = 0.0f;

    [Header("Physical RPM range")]
    public float rpmMin = 600f;
    public float rpmMax = 2200f;
    public float currentRPM = 600f;

    [Header("Layers")]
    public EngineLayer[] layers;

    public float boatSpeed;
    
    public bool isReversing;

    


    private void Awake()
    {
        if (layers == null || layers.Length == 0)
            layers = GetComponentsInChildren<EngineLayer>();

        foreach (var layer in layers)
            layer.Init();
    }

    private void Update()
    {
        // Load is simple for now — throttle drives it
        //engineLoad = throttle;

        // Update all layers
        foreach (var layer in layers)
            layer.UpdateLayer(engineRPM, engineLoad);
    }

    public void SetThrottle(float value)
    {
        throttle = Mathf.Clamp01(value);
    }

    public void SetRPMFromPhysical(float rpmValue)
    {
        currentRPM = rpmValue;

        // Direction: forward vs reverse
        isReversing = rpmValue < 0f;

        // Magnitude: always 0–1 for the curves
        float magnitude = Mathf.Abs(rpmValue);
        engineRPM = Mathf.InverseLerp(rpmMin, rpmMax, magnitude);
    }

    public void SetLoad(float loadValue)
    {
        engineLoad = Mathf.Clamp01(loadValue);
    }

    

    public void SetSpeed(float s)
    {
        boatSpeed = s;
    }

    public void SetReverse(bool r)
    {
        isReversing = r;
    }

}