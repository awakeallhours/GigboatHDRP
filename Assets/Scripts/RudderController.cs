using UnityEngine;

public class RudderController : MonoBehaviour
{
    [SerializeField] private GigboatMovement movement;
    [SerializeField] private float maxVisualAngle = 30f;
    [SerializeField] private float visualTurnSpeed = 5f;

    private float currentAngle;

    void Update()
    {
        // Movement.RudderAngle is already -1 to +1
        float targetAngle = movement.RudderAngle * maxVisualAngle;

        currentAngle = Mathf.Lerp(
            currentAngle,
            targetAngle,
            Time.deltaTime * visualTurnSpeed
        );

        transform.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
    }
}
