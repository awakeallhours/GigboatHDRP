using UnityEngine;

public sealed class ProbeDepthProvider
{
    private readonly WaterProbeSampler sampler;

    public ProbeDepthProvider(WaterProbeSampler sampler)
    {
        this.sampler = sampler;
    }

    public int ProbeCount => sampler.ProbeCount;

    public bool[] Valid => sampler.PointValid;
    public float[] Heights => sampler.PointHeights;
    public Vector3[] Normals => sampler.PointNormals;
    public Transform[] Points => sampler.SamplePoints;
    public ProbeType[] Types => sampler.ProbeTypes;
}