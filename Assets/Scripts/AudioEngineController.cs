using UnityEngine;

/// <summary>
/// Audio-facing controller that receives normalized engine state
/// (RPM, load, throttle, reverse, speed) and updates audio layers.
/// This class contains NO physical engine logic.
/// </summary>
public class AudioEngineController : MonoBehaviour
{
    // ---------------------------------------------------------
    // NORMALIZED ENGINE STATE (0–1)
    // ---------------------------------------------------------
    [Header("Normalized Engine State (0–1)")]
    [Range(0f, 1f)][SerializeField] private float engineRPM = 0f;
    [Range(0f, 1f)][SerializeField] private float engineLoad = 0f;
    [Range(0f, 1f)][SerializeField] private float throttle = 0f;

    // ---------------------------------------------------------
    // ADDITIONAL AUDIO INPUTS
    // ---------------------------------------------------------
    [Header("Additional Audio Inputs")]
    [Tooltip("True if the engine is running in reverse.")]
    [SerializeField] private bool isReversing = false;

    [Tooltip("Boat speed in m/s (used for prop/cavitation layers).")]
    [SerializeField] private float boatSpeed = 0f;

    // ---------------------------------------------------------
    // AUDIO LAYERS
    // ---------------------------------------------------------
    [Header("Audio Layers")]
    [SerializeField] private AudioEngineLayer[] layers;

    private void Awake()
    {
        if (layers == null || layers.Length == 0)
            layers = GetComponentsInChildren<AudioEngineLayer>();

        foreach (var layer in layers)
            layer.Init();
    }

    private void Update()
    {
        foreach (var layer in layers)
            layer.UpdateLayer(engineRPM, engineLoad, throttle, isReversing, boatSpeed);
    }

    // ---------------------------------------------------------
    // PUBLIC API (CALLED BY MarinePowertrainController)
    // ---------------------------------------------------------
    public void SetRPM01(float value) => engineRPM = Mathf.Clamp01(value);
    public void SetLoad01(float value) => engineLoad = Mathf.Clamp01(value);
    public void SetThrottle01(float value) => throttle = Mathf.Clamp01(value);
    public void SetReverse(bool value) => isReversing = value;
    public void SetSpeed(float value) => boatSpeed = Mathf.Max(0f, value);

    // Optional getters if needed by debug UI
    public float RPM01 => engineRPM;
    public float Load01 => engineLoad;
    public float Throttle01 => throttle;
    public bool IsReversing => isReversing;
    public float BoatSpeed => boatSpeed;
}