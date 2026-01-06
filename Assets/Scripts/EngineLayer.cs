using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class EngineLayer : MonoBehaviour
{
    [Header("Core")]
    public EngineController engine;
    public string layerName = "Idle";
    public bool enabledLayer = true;

    [Tooltip("How volume responds to normalized RPM (0–1).")]
    public AnimationCurve volumeByRPM = AnimationCurve.Linear(0, 1, 1, 1);

    [Tooltip("How volume responds to normalized Load (0–1). Multiplies RPM volume.")]
    public AnimationCurve volumeByLoad = AnimationCurve.Linear(0, 1, 1, 1);

    [Tooltip("How pitch responds to normalized RPM (0–1).")]
    public AnimationCurve pitchByRPM = AnimationCurve.Linear(0, 1, 1, 1);

    [Tooltip("Base pitch multiplier before the curve.")]
    public float basePitch = 1.0f;

    [Tooltip("Global volume multiplier for this layer.")]
    [Range(0f, 1f)]
    public float layerVolume = 1.0f;

    private AudioSource _source;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.loop = true;
        _source.playOnAwake = false;
    }

    public void Init()
    {
        if (_source == null)
            _source = GetComponent<AudioSource>();

        if (_source == null)
        {
            Debug.LogError($"EngineLayer '{layerName}' is missing an AudioSource.");
            enabledLayer = false;
            return;
        }

        _source.loop = true;
        _source.playOnAwake = false;

        if (!_source.isPlaying && enabledLayer)
            _source.Play();
    }

    public void UpdateLayer(float rpmNormalized, float loadNormalized)
    {
        if (!enabledLayer)
        {
            if (_source.isPlaying)
                _source.Stop();
            return;
        }

        // Forward layers OFF when reversing
        if (engine.isReversing && layerName.Contains("Forward"))
        {
            _source.volume = 0f;
            return;
        }

        // Reverse layers OFF when not reversing
        if (!engine.isReversing && layerName.Contains("Reverse"))
        {
            _source.volume = 0f;
            return;
        }

        rpmNormalized = Mathf.Clamp01(rpmNormalized);
        loadNormalized = Mathf.Clamp01(loadNormalized);

        // --- Base RPM-driven volume ---
        float volRPM = volumeByRPM.Evaluate(rpmNormalized);

        // --- Load-driven volume (instant throttle response) ---
        float volLoad = volumeByLoad.Evaluate(loadNormalized);

        // --- ORIGINAL behaviour (RPM × Load) ---
        float finalVolume = volRPM * volLoad * layerVolume;

        // --- OPTIONAL: Blend RPM + Load for snappier throttle response ---
         finalVolume = Mathf.Lerp(volRPM, volLoad, 0.5f) * layerVolume;

        // --- OPTIONAL: Aggressive throttle snap (load-dominant) ---
        // finalVolume = (volRPM * 0.3f + volLoad * 0.7f) * layerVolume;

        _source.volume = finalVolume;

        // Pitch always follows RPM (physical inertia)
        float pitchCurve = pitchByRPM.Evaluate(rpmNormalized);
        _source.pitch = basePitch * pitchCurve;
    }
}