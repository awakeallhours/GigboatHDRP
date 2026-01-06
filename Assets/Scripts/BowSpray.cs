using UnityEngine;

public class BowSpray : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;

    [Header("Water FX – Bow Spray")]
    [SerializeField] private ParticleSystem[] bowSprays;
    [SerializeField] private float maxBowSprayRate = 50f;
    [SerializeField] private float bowSprayMaxSpeed = 20f;

    [Header("Behaviour")]
    [SerializeField] private bool useForwardOnly = true;

    [Header("Shape Tuning")]
    [SerializeField] private float minSprayAngle = 10f;
    [SerializeField] private float maxSprayAngle = 40f;

    [SerializeField] private float minSprayRadius = 0.1f;
    [SerializeField] private float maxSprayRadius = 0.3f;


    private ParticleSystem.EmissionModule[] emissions;
    private ParticleSystem.ShapeModule[] shapes;

    private int validCount;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponentInParent<Rigidbody>();

        int count = bowSprays.Length;
        emissions = new ParticleSystem.EmissionModule[count];
        shapes = new ParticleSystem.ShapeModule[count];

        for (int i = 0; i < count; i++)
        {
            emissions[i] = bowSprays[i].emission;
            shapes[i] = bowSprays[i].shape;
        }

        validCount = count; // <-- THIS WAS MISSING
    }


    private void Update()
    {
        HandleBowSpray();
        ApplyShapeSettings();
    }

    private void HandleBowSpray()
    {
        if (emissions == null || emissions.Length == 0)
            return;

        float speed;

        if (useForwardOnly)
        {
            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            speed = Mathf.Max(0f, forwardSpeed);
        }
        else
        {
            speed = rb.linearVelocity.magnitude;
        }

        float speed01 = Mathf.Clamp01(speed / bowSprayMaxSpeed);
        float rate = speed01 * maxBowSprayRate;

        for (int i = 0; i < emissions.Length; i++)
            emissions[i].rateOverTime = rate;
    }


    private void ApplyShapeSettings()
    {
        if (validCount == 0)
            return;

        // Calculate speed01 again (safe, cheap, and avoids cross‑method coupling)
        float speed = useForwardOnly
            ? Mathf.Max(0f, Vector3.Dot(rb.linearVelocity, transform.forward))
            : rb.linearVelocity.magnitude;

        float speed01 = Mathf.Clamp01(speed / bowSprayMaxSpeed);

        float angle = Mathf.Lerp(minSprayAngle, maxSprayAngle, speed01);
        float radius = Mathf.Lerp(minSprayRadius, maxSprayRadius, speed01);

        for (int i = 0; i < validCount; i++)
        {
            shapes[i].angle = angle;
            shapes[i].radius = radius;
        }
    }
}


