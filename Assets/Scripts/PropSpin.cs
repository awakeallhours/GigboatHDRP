using UnityEngine;

public class PropSpin : MonoBehaviour
{
    [SerializeField] private GigboatMovement movement;

    [Header("Input")]
    public float throttle; // 0–1 from your UI or input system

    [Header("RPM Settings")]
    public float idleRPM = 300f;
    public float maxRPM = 3000f;
    public float spinAcceleration = 5f;

    [Header("Debug")]
    public float currentRPM;

    void Update()
    {
        // Convert throttle (-100 to +100) into -1 to +1
        float throttle01 = movement.ThrottlePercent / 100f;

        // Engine tries to reach a target RPM based on throttle
        float desiredRPM = Mathf.Lerp(idleRPM, maxRPM, Mathf.Abs(throttle01));

        // Water load reduces RPM at low speed (realistic bogging)
        float speed = movement.RB.linearVelocity.magnitude;
        float loadFactor = Mathf.Clamp01(speed / 15f); // tune 15f to taste
        float loadedRPM = Mathf.Lerp(desiredRPM * 0.4f, desiredRPM, loadFactor);

        // Apply sign for forward/reverse spin
        float targetRPM = loadedRPM * Mathf.Sign(throttle01);

        // Smooth RPM change
        currentRPM = Mathf.Lerp(currentRPM, targetRPM, Time.deltaTime * spinAcceleration);

        // Convert RPM to degrees per second
        float degreesPerSecond = (currentRPM / 60f) * 360f;

        // Spin around local Z
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime, Space.Self);
    }


}
