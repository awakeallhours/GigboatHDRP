using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Axiom.Physics.Units;

public class MarineProp : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform propPoint;
    [SerializeField] private DistanceValue referenceDepth;

    private WaterSurface water;

    [Header("Debug (Read Only)")]
    [SerializeField] private float immersion01;
    [SerializeField] private float rawDepthMeters;
    [SerializeField] private bool valid;

    public float PropImmersion01 => immersion01;
    public bool PropIsUnderwater => immersion01 > 0.01f;

    private void Awake()
    {
        water = FindFirstObjectByType<WaterSurface>();

        if (propPoint == null)
            Debug.LogWarning("[MarineProp] No propPoint assigned.");
    }

    private void Update()
    {
        if (water == null || propPoint == null)
        {
            immersion01 = 0f;
            valid = false;
            return;
        }

        WaterSearchParameters wp = new WaterSearchParameters();
        WaterSearchResult wr;

        wp.startPositionWS = propPoint.position;
        wp.targetPositionWS = propPoint.position;
        wp.error = 0.01f;
        wp.maxIterations = 8;

        valid = water.ProjectPointOnWaterSurface(wp, out wr);

        if (!valid)
        {
            immersion01 = 0f;
            rawDepthMeters = 0f;
            return;
        }

        float waterY = wr.projectedPositionWS.y;
        float pointY = propPoint.position.y;

        rawDepthMeters = waterY - pointY;
        immersion01 = Mathf.Clamp01(rawDepthMeters / referenceDepth.ValueMeters);
    }
}
