using UnityEngine;

public class WakeTrail : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;

    // One anchor for all wake trails
    [SerializeField] private Transform wakeAnchor;

    // Multiple particle systems
    [SerializeField] private ParticleSystem[] wakeParticles;

    [Header("Wake Settings")]
    [SerializeField] private float idleEmission = 0f;
    [SerializeField] private float maxEmissionRate = 10f;
    [SerializeField] private float maxWakeSpeed = 20f;
    [SerializeField] private float minWidth = 0.2f;
    [SerializeField] private float maxWidth = 1.0f;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponentInParent<Rigidbody>();
    }

    private void LateUpdate()
    {
        MaintainAnchorPositions();
        HandleWake();
    }

    private void MaintainAnchorPositions()
    {
        if (wakeAnchor == null)
            return;

        Vector3 anchorPos = wakeAnchor.position;

        for (int i = 0; i < wakeParticles.Length; i++)
        {
            if (wakeParticles[i] != null)
                wakeParticles[i].transform.position = anchorPos;
        }
    }

    private void HandleWake()
    {
        float speed = rb.linearVelocity.magnitude;
        float speed01 = Mathf.Clamp01(speed / maxWakeSpeed);

        for (int i = 0; i < wakeParticles.Length; i++)
        {
            if (wakeParticles[i] == null)
                continue;

            // Get modules fresh each frame (Unity requirement)
            var emission = wakeParticles[i].emission;
            var trails = wakeParticles[i].trails;

            emission.rateOverTime = idleEmission + (speed01 * maxEmissionRate);
            trails.widthOverTrail = Mathf.Lerp(minWidth, maxWidth, speed01);
        }
    }
}