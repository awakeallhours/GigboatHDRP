using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class GigboatSpawner : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────
    [Header("References")]

    [Tooltip("Prefab of the gigboat to spawn.")]
    [SerializeField] private GameObject gigboatPrefab;

    [Tooltip("Spawn point for the boat.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Debug UI for buoyancy probes.")]
    [SerializeField] private BuoyancyDebugUI debugUI;

    [Tooltip("UI for steering/throttle display.")]
    [SerializeField] private GigboatUI steeringUI;

    [Tooltip("Cinemachine camera that should follow the spawned boat.")]
    [SerializeField] private CinemachineCamera cineCam;


    // ─────────────────────────────────────────────────────────────
    // SETTINGS
    // ─────────────────────────────────────────────────────────────
    [Header("Respawn Settings")]
    [Tooltip("Delay before respawning the boat after capsizing.")]
    [SerializeField] private float respawnDelay = 1.5f;


    // ─────────────────────────────────────────────────────────────
    // INTERNAL STATE
    // ─────────────────────────────────────────────────────────────
    private bool isRespawning = false;
    private GameObject currentBoat;


    // ─────────────────────────────────────────────────────────────
    // UNITY EVENTS
    // ─────────────────────────────────────────────────────────────
    private void Start()
    {
        SpawnBoat();
    }

    private void Update()
    {
        if (currentBoat == null)
            return;

        // Detect capsizing
        float roll = Mathf.DeltaAngle(0f, currentBoat.transform.eulerAngles.z);

        if (Mathf.Abs(roll) > 90f)
        {
            StartRespawn();
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            StartRespawn();
        }
    }


    // ─────────────────────────────────────────────────────────────
    // RESPAWN LOGIC
    // ─────────────────────────────────────────────────────────────
    public void RespawnBoat()
    {
        if (currentBoat != null)
            Destroy(currentBoat);

        SpawnBoat();
    }

    private void StartRespawn()
    {
        if (isRespawning)
            return;

        isRespawning = true;
        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        RespawnBoat();
        isRespawning = false;
    }


    // ─────────────────────────────────────────────────────────────
    // SPAWNING
    // ─────────────────────────────────────────────────────────────
    private void SpawnBoat()
    {
        if (gigboatPrefab == null || spawnPoint == null)
        {
            Debug.LogError("GigboatSpawner: Missing prefab or spawn point.");
            return;
        }

        // Spawn slightly above water to avoid buoyancy explosion
        Vector3 spawnPos = spawnPoint.position;

        currentBoat = Instantiate(gigboatPrefab, spawnPos, spawnPoint.rotation);

        // Cache components
        var movement = currentBoat.GetComponent<GigboatMovement>();
        var rb = currentBoat.GetComponent<Rigidbody>();
        var debugProbe = currentBoat.GetComponent<GigboatDebugProbe>();

        if (movement == null || rb == null)
        {
            Debug.LogError("GigboatSpawner: Spawned boat missing required components.");
            return;
        }

        // Assign camera target
        if (cineCam != null && movement.CameraTarget != null)
        {
            cineCam.Target.TrackingTarget = movement.CameraTarget;
        }

        // Reset physics state
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Enable buoyancy next fixed update
        StartCoroutine(EnableBuoyancyNextFixedUpdate(currentBoat));

#if UNITY_EDITOR
        UnityEditor.Selection.activeGameObject = currentBoat;
#endif

        // UI hookups
        if (steeringUI != null)
            steeringUI.SetBoat(movement);

        if (debugUI != null && debugProbe != null)
            debugUI.SetBoat(debugProbe);
    }


    // ─────────────────────────────────────────────────────────────
    // BUOYANCY SAFETY DELAY
    // ─────────────────────────────────────────────────────────────
    private IEnumerator EnableBuoyancyNextFixedUpdate(GameObject boat)
    {
        var buoy = boat.GetComponent<Buoyancy>();
        if (buoy == null)
            yield break;

        buoy.enabled = false;
        yield return new WaitForFixedUpdate();
        buoy.enabled = true;
    }
}