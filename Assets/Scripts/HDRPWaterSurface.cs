using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class HDRPWaterSurface : MonoBehaviour, IWaterSurface
{
    private WaterSurface water;

    private void Awake()
    {
        water = FindFirstObjectByType<WaterSurface>();
    }

    public bool TryGetHeightAndNormal(
        Vector3 worldPosition,
        out float waterHeight,
        out Vector3 waterNormal)
    {
        waterHeight = 0f;
        waterNormal = Vector3.up;

        if (water == null)
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