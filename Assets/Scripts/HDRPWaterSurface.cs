using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;

public class HDRPWaterSurface : MonoBehaviour, IWaterSurface
{
    private WaterSurface water;
    private bool ready = false;

    private IEnumerator Start()
    {
        // Wait one frame so HDRP can initialise the water simulation
        yield return null;

        water = FindFirstObjectByType<WaterSurface>();

        if (water == null)
        {
            Debug.LogError("HDRPWaterSurface: No WaterSurface found in scene.");
            yield break;
        }

        ready = true;
    }

    public bool TryGetHeightAndNormal(
        Vector3 worldPosition,
        out float waterHeight,
        out Vector3 waterNormal)
    {
        waterHeight = 0f;
        waterNormal = Vector3.up;

        if (!ready || water == null)
            return false;

        WaterSearchParameters wp = new WaterSearchParameters
        {
            startPositionWS = worldPosition,
            targetPositionWS = worldPosition,
            error = 0.01f,
            maxIterations = 8
        };

        if (water.ProjectPointOnWaterSurface(wp, out WaterSearchResult wr))
        {
            waterHeight = wr.projectedPositionWS.y;
            waterNormal = wr.normalWS;
            return true;
        }

        return false;
    }
}