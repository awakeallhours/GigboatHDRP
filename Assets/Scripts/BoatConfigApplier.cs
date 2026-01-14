using UnityEngine;

public class BoatConfigApplier : MonoBehaviour
{
    [SerializeField] private BoatConfig config;

    [Header("Module References")]
    [SerializeField] private Buoyancy buoyancy;
    [SerializeField] private FollowCam followCam;

    private void Start()
    {
        ApplyAll();
    }

    public void ApplyAll()
    {
        if (config == null)
        {
            Debug.LogWarning("No BoatConfig assigned.");
            return;
        }

        ApplyBuoyancy();
        ApplyFollowCam();
    }

    private void ApplyBuoyancy()
    {
        if (buoyancy != null)
        {
            buoyancy.ApplyBuoyancyConfig(config.buoyancy);
        }
    }

    private void ApplyFollowCam()
    {
        if (followCam != null)
            followCam.ApplyFollowCamConfig(config.followCam);
    }
}