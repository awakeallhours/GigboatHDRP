using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class WaterProbeSampler : MonoBehaviour
{
    [Header("Probe Points")]
    [SerializeField] private Transform[] samplePoints;

    private WaterSurface water;

    private float[] pointHeights;
    private Vector3[] pointNormals;
    private bool[] pointValid;

    public int ProbeCount => samplePoints.Length;

    private void Awake()
    {
        water = FindFirstObjectByType<WaterSurface>();

        pointHeights = new float[samplePoints.Length];
        pointNormals = new Vector3[samplePoints.Length];
        pointValid = new bool[samplePoints.Length];
    }

    private void FixedUpdate()
    {
        if (water == null)
            return;

        for (int i = 0; i < samplePoints.Length; i++)
            SampleProbe(i);
    }

    private void SampleProbe(int index)
    {
        Transform p = samplePoints[index];

        WaterSearchParameters wp = new WaterSearchParameters
        {
            startPositionWS = p.position,
            targetPositionWS = p.position,
            error = 0.01f,
            maxIterations = 8
        };

        bool ok = water.ProjectPointOnWaterSurface(wp, out WaterSearchResult wr);
        pointValid[index] = ok;

        if (!ok)
            return;

        pointHeights[index] = wr.projectedPositionWS.y;
        pointNormals[index] = wr.normalWS;
    }

    public void GetProbeData(out bool[] valid, out float[] heights, out Vector3[] normals, out Transform[] points)
    {
        valid = pointValid;
        heights = pointHeights;
        normals = pointNormals;
        points = samplePoints;
    }
}