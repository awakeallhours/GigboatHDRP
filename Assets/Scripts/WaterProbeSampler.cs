using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;

public class WaterProbeSampler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AxiomBuoyancyVessel vessel;

    private WaterSurface water;
    private bool waterReady = false;

    private Transform[] samplePoints;
    private ProbeType[] probeTypes;

    private float[] pointHeights;
    private Vector3[] pointNormals;
    private bool[] pointValid;

    public bool[] PointValid => pointValid;
    public float[] PointHeights => pointHeights;
    public Vector3[] PointNormals => pointNormals;
    public Transform[] SamplePoints => samplePoints;
    public ProbeType[] ProbeTypes => probeTypes;

    public int ProbeCount => samplePoints?.Length ?? 0;

    private void Awake()
    {
        if (vessel == null)
        {
            Debug.LogError("WaterProbeSampler: No vessel assigned.");
            return;
        }

        var probes = vessel.ProbeObjects;
        int count = probes.Count;

        samplePoints = new Transform[count];
        probeTypes = new ProbeType[count];

        for (int i = 0; i < count; i++)
        {
            samplePoints[i] = probes[i];

            if (probes[i].parent == vessel.KeelProbeRoot)
                probeTypes[i] = ProbeType.Keel;
            else if (probes[i].parent == vessel.SideProbeRoot)
                probeTypes[i] = ProbeType.Side;
            else
                probeTypes[i] = ProbeType.Deck;
        }

        pointHeights = new float[count];
        pointNormals = new Vector3[count];
        pointValid = new bool[count];
    }

    private IEnumerator Start()
    {
        // Wait one frame for HDRP water to initialise
        yield return null;

        water = FindFirstObjectByType<WaterSurface>();

        Debug.Log($"[Sampler] WaterSurface found = {water}");

        if (water == null)
            yield break;

        waterReady = true;
    }

    private void FixedUpdate()
    {
        if (!waterReady || water == null || samplePoints == null)
            return;

        for (int i = 0; i < samplePoints.Length; i++)
            SampleProbe(i);
    }

    private void SampleProbe(int index)
    {
        Transform p = samplePoints[index];

        if (index == 0)
            Debug.Log($"[Sampler] Probe[0] world pos = {p.position}");

        WaterSearchParameters wp = new WaterSearchParameters
        {
            startPositionWS = p.position,
            targetPositionWS = p.position,
            error = 0.01f,
            maxIterations = 8
        };

        bool ok = water.ProjectPointOnWaterSurface(wp, out WaterSearchResult wr);
        pointValid[index] = ok;

        if (index == 0)
            Debug.Log($"[Sampler] Probe[0] ok = {ok}");

        if (!ok)
            return;

        pointHeights[index] = wr.projectedPositionWS.y;
        pointNormals[index] = wr.normalWS;
    }

    public void GetProbeData(
        out bool[] valid,
        out float[] heights,
        out Vector3[] normals,
        out Transform[] points,
        out ProbeType[] types)
    {
        valid = pointValid;
        heights = pointHeights;
        normals = pointNormals;
        points = samplePoints;
        types = probeTypes;
    }
}