using UnityEngine;

public class WakeTrail : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private ParticleSystem wakeParticles;

    [Header("Wake Settings")]
    [SerializeField] private float idleEmission = 0f;
    [SerializeField] private float maxEmissionRate = 10f;
    [SerializeField] private float maxWakeSpeed = 20f;
    [SerializeField] private float minWidth = 0.2f;
    [SerializeField] private float maxWidth = 1.0f;

    private ParticleSystem.EmissionModule emission;
    private ParticleSystem.TrailModule trails;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponentInParent<Rigidbody>();

        emission = wakeParticles.emission;
        trails = wakeParticles.trails;
    }

    private void Update()
    {
        HandleWake();
    }

    private void HandleWake()
    {
        float speed = rb.linearVelocity.magnitude;
        float speed01 = Mathf.Clamp01(speed / maxWakeSpeed);

        // Emission
        emission.rateOverTime = idleEmission + (speed01 * maxEmissionRate);


        // Width
        trails.widthOverTrail = Mathf.Lerp(minWidth, maxWidth, speed01);
    }
}

