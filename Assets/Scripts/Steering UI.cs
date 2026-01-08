using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GigboatUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("Boat Reference")]
    [Tooltip("The boat whose movement values drive the UI.")]
    [SerializeField] private GigboatMovement gigboat;

    [Header("UI Elements")]
    [Tooltip("Slider showing rudder position (-1 to +1).")]
    [SerializeField] private Slider steeringSlider;

    [Tooltip("Slider showing throttle percentage (-100 to +100).")]
    [SerializeField] private Slider throttleSlider;

    //[SerializeField] private float maxRotation = 90f; // this will be for setting the steering wheel angle when that is used instead of a slider

    [Tooltip("Text element showing boat speed in knots.")]
    [SerializeField] private TextMeshProUGUI speedText;


    // ─────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────
    public void SetBoat(GigboatMovement newBoat)
    {
        gigboat = newBoat;
    }


    // ─────────────────────────────────────────────────────────────
    // UNITY EVENTS
    // ─────────────────────────────────────────────────────────────
    private void Start()
    {
        // Steering slider fallback
        if (steeringSlider == null)
            steeringSlider = GetComponent<Slider>();

        if (steeringSlider != null)
        {
            steeringSlider.minValue = -1f;
            steeringSlider.maxValue = 1f;
        }

        // Throttle slider fallback
        if (throttleSlider == null)
            throttleSlider = GetComponent<Slider>();

        if (throttleSlider != null)
        {
            throttleSlider.minValue = -100f;
            throttleSlider.maxValue = 100f;
        }
    }

    private void Update()
    {
        if (gigboat == null)
            return;

        UpdateSteering();
        UpdateThrottle();
    }


    // ─────────────────────────────────────────────────────────────
    // UI UPDATE METHODS
    // ─────────────────────────────────────────────────────────────
    private void UpdateSteering()
    {
        if (steeringSlider == null)
            return;

        steeringSlider.value = gigboat.RudderAngle;
    }

    private void UpdateThrottle()
    {
        if (throttleSlider != null)
            throttleSlider.value = gigboat.ThrottlePercent;

        if (speedText != null)
            speedText.text = $"Speed {gigboat.SpeedKnots:F1} Kn";
    }
}