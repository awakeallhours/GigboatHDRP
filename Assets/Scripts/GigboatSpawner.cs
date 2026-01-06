using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class GigboatSpawner : MonoBehaviour
{
    [SerializeField] private GameObject gigboatPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private BuoyancyDebugUI debugUI;
    [SerializeField] private float respawnDelay = 1.5f;
    [SerializeField] private CinemachineCamera cineCam;
    [SerializeField] private GigboatUI steeringUI;

    private bool isRespawning = false;
    private GameObject currentBoat;

    public Transform CameraTarget;

    void Start()
    {
        SpawnBoat();
    }

    void Update()
    {
        if (currentBoat == null) return;

        float roll = currentBoat.transform.eulerAngles.z;
        roll = Mathf.DeltaAngle(0, roll);

        if (Mathf.Abs(roll) > 90f)
            StartRespawn();
        else if (Input.GetKeyDown(KeyCode.R))
            StartRespawn();
    }

    public void RespawnBoat()
    {
        if (currentBoat != null)
        {
            Destroy(currentBoat);
        }

        SpawnBoat();
    }

    private void SpawnBoat()
    {
        // Spawn slightly above water to prevent buoyancy explosion
        Vector3 spawnPos = spawnPoint.position + Vector3.up * 0.0f;

        currentBoat = Instantiate(gigboatPrefab, spawnPos, spawnPoint.rotation);
        cineCam.Target.TrackingTarget = currentBoat.GetComponent<GigboatMovement>().CameraTarget;
        
        // Reset physics state to avoid inherited prefab velocity
        Rigidbody rb = currentBoat.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        StartCoroutine(EnableBuoyancyNextFixedUpdate(currentBoat));

#if UNITY_EDITOR
        UnityEditor.Selection.activeGameObject = currentBoat;
#endif

        
        steeringUI.SetBoat(currentBoat.GetComponent<GigboatMovement>());
        debugUI.SetBoat(currentBoat.GetComponent<GigboatDebugProbe>());
    }

    private void StartRespawn()
    {
        if (isRespawning) return;

        isRespawning = true;
        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        RespawnBoat();
        isRespawning = false;
    }

    private IEnumerator EnableBuoyancyNextFixedUpdate(GameObject boat)
    {
        var buoy = boat.GetComponent<Buoyancy>();
        buoy.enabled = false;

        yield return new WaitForFixedUpdate();

        buoy.enabled = true;

    }
}
