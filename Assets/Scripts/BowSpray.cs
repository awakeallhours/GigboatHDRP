using UnityEngine;

/// <summary>
/// Controls bow spray particle emission and shape based on vessel speed.
/// </summary>
public class BowSpray : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;

    [Header("Water FX – Bow Spray")]
    [Tooltip("All bow spray particle systems.")]
    [SerializeField] private ParticleSystem[] bowSprays;

    [Tooltip("Maximum emission rate at full speed.")]
    [SerializeField] private float maxBowSprayRate = 50f;

    [Tooltip("Speed (m/s) at which spray reaches maximum intensity.")]
    [SerializeField] private float bowSprayMaxSpeed = 20f;

    [Header("Behaviour")]
    [Tooltip("If true, only forward velocity contributes to spray.")]
    [SerializeField] private bool useForwardOnly = true;

    [Header("Shape Tuning")]
    [SerializeField] private float minSprayAngle = 10f;
    [SerializeField] private float maxSprayAngle = 40f;

    [SerializeField] private float minSprayRadius = 0.1f;
    [SerializeField] private float maxSprayRadius = 0.3f;

    private ParticleSystem.EmissionModule[] emissions;
    private ParticleSystem.ShapeModule[] shapes;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponentInParent<Rigidbody>();

        int count = bowSprays.Length;

        if (count == 0)
            return;

        emissions = new ParticleSystem.EmissionModule[count];
        shapes = new ParticleSystem.ShapeModule[count];

        for (int i = 0; i < count; i++)
        {
            if (bowSprays[i] == null)
                continue;

            emissions[i] = bowSprays[i].emission;
            shapes[i] = bowSprays[i].shape;
        }
    }

    private void Update()
    {
        if (bowSprays.Length == 0)
            return;

        float speed01 = GetSpeed01();
        HandleBowSpray(speed01);
        ApplyShapeSettings(speed01);
    }

    private float GetSpeed01()
    {
        float speed = useForwardOnly
            ? Mathf.Max(0f, Vector3.Dot(rb.linearVelocity, transform.forward))
            : rb.linearVelocity.magnitude;

        return Mathf.Clamp01(speed / bowSprayMaxSpeed);
    }

    private void HandleBowSpray(float speed01)
    {
        float rate = speed01 * maxBowSprayRate;

        for (int i = 0; i < emissions.Length; i++)
            emissions[i].rateOverTime = rate;
    }

    private void ApplyShapeSettings(float speed01)
    {
        float angle = Mathf.Lerp(minSprayAngle, maxSprayAngle, speed01);
        float radius = Mathf.Lerp(minSprayRadius, maxSprayRadius, speed01);

        for (int i = 0; i < shapes.Length; i++)
        {
            shapes[i].angle = angle;
            shapes[i].radius = radius;
        }
    }
}