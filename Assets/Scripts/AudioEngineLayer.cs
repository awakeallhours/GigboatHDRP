using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioEngineLayer : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // CORE
    // ─────────────────────────────────────────────────────────────
    [Header("Core")]
    [Tooltip("Reference to the audio engine controller providing normalized values.")]
    [SerializeField] private AudioEngineController audioEngine;

    [Tooltip("Name of this layer (e.g., Idle, ForwardLow, ReverseHigh).")]
    [SerializeField] private string layerName = "Idle";

    [Tooltip("If false, this layer is disabled entirely.")]
    [SerializeField] private bool enabledLayer = true;


    // ─────────────────────────────────────────────────────────────
    // CURVES
    // ─────────────────────────────────────────────────────────────
    [Header("Volume & Pitch Curves")]
    [Tooltip("How volume responds to normalized RPM (0–1).")]
    [SerializeField] private AnimationCurve volumeByRPM = AnimationCurve.Linear(0, 1, 1, 1);

    [Tooltip("How volume responds to normalized Load (0–1). Multiplies RPM volume.")]
    [SerializeField] private AnimationCurve volumeByLoad = AnimationCurve.Linear(0, 1, 1, 1);

    [Tooltip("How pitch responds to normalized RPM (0–1).")]
    [SerializeField] private AnimationCurve pitchByRPM = AnimationCurve.Linear(0, 1, 1, 1);

    [Tooltip("Base pitch multiplier before the curve.")]
    [SerializeField] private float basePitch = 1.0f;

    [Tooltip("Global volume multiplier for this layer.")]
    [Range(0f, 1f)]
    [SerializeField] private float layerVolume = 1.0f;


    // ─────────────────────────────────────────────────────────────
    // INTERNAL
    // ─────────────────────────────────────────────────────────────
    private AudioSource source;


    // ─────────────────────────────────────────────────────────────
    // INITIALIZATION
    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
    }

    public void Init()
    {
        if (source == null)
            source = GetComponent<AudioSource>();

        if (source == null)
        {
            Debug.LogError($"AudioEngineLayer '{layerName}' is missing an AudioSource.");
            enabledLayer = false;
            return;
        }

        source.loop = true;
        source.playOnAwake = false;

        if (!source.isPlaying && enabledLayer)
            source.Play();
    }


    // ─────────────────────────────────────────────────────────────
    // UPDATE LAYER (NEW SIGNATURE)
    // ─────────────────────────────────────────────────────────────
    public void UpdateLayer(
        float rpm01,
        float load01,
        float throttle01,
        bool isReversing,
        float boatSpeed
    )
    {
        if (!enabledLayer)
        {
            if (source.isPlaying)
                source.Stop();
            return;
        }

        // ─────────────────────────────────────────────────────────
        // LAYER ENABLE/DISABLE BASED ON DIRECTION
        // ─────────────────────────────────────────────────────────

        bool isForwardLayer = layerName.Contains("Forward");
        bool isReverseLayer = layerName.Contains("Reverse");

        if (isForwardLayer && isReversing)
        {
            source.volume = 0f;
            return;
        }

        if (isReverseLayer && !isReversing)
        {
            source.volume = 0f;
            return;
        }

        // Clamp inputs
        rpm01 = Mathf.Clamp01(rpm01);
        load01 = Mathf.Clamp01(load01);

        // ─────────────────────────────────────────────────────────
        // VOLUME
        // ─────────────────────────────────────────────────────────

        float volRPM = volumeByRPM.Evaluate(rpm01);
        float volLoad = volumeByLoad.Evaluate(load01);

        // Original behaviour (RPM × Load)
        float finalVolume = volRPM * volLoad * layerVolume;

        // Optional blend (snappier throttle response)
        finalVolume = Mathf.Lerp(volRPM, volLoad, 0.5f) * layerVolume;

        source.volume = finalVolume;

        // ─────────────────────────────────────────────────────────
        // PITCH
        // ─────────────────────────────────────────────────────────

        float pitchCurve = pitchByRPM.Evaluate(rpm01);
        source.pitch = basePitch * pitchCurve;
    }
}